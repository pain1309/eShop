using System.Diagnostics;

namespace Microsoft.AspNetCore.Hosting;

internal static class MigrateDbContextExtensions
{
    private static readonly string ActivitySourceName = "DbMigrations";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    // IServiceCollection là interface chính để đăng ký các service vào DI container
    // Phương thức mở rộng này cho phép:
    // 1. Thêm migration service vào DI container một cách fluent
    // 2. Đăng ký các dependency cần thiết cho việc migration
    // 3. Tích hợp với hệ thống dependency injection có sẵn của ứng dụng
    public static IServiceCollection AddMigration<TContext>(this IServiceCollection services)
        where TContext : DbContext
        // Đây là phương thức overload đơn giản nhất của AddMigration
        // Nó chuyển tiếp (delegate) sang phiên bản đầy đủ hơn của AddMigration bằng cách:
        // 1. Truyền vào một lambda expression (_, _) => Task.CompletedTask làm seeder
        // 2. Dấu gạch dưới (_) biểu thị tham số không được sử dụng (TContext và IServiceProvider)
        // 3. Task.CompletedTask được dùng làm seeder rỗng vì không cần thực hiện seeding
        // => Đây là cách viết ngắn gọn khi chỉ cần migrate mà không cần seed dữ liệu
        => services.AddMigration<TContext>((_, _) => Task.CompletedTask);

    // Phương thức mở rộng này cho phép đăng ký migration service với một seeder tùy chỉnh
    // Tham số:
    // - services: IServiceCollection để đăng ký các service
    // - seeder: Hàm delegate để thực hiện seeding dữ liệu sau khi migrate
    // Mục đích:
    // 1. Cấu hình OpenTelemetry để theo dõi quá trình migration thông qua ActivitySource
    // 2. Đăng ký MigrationHostedService như một hosted service để:
    //    - Tự động thực hiện migration khi ứng dụng khởi động
    //    - Chạy seeder được cung cấp sau khi migration hoàn tất
    //    - Đảm bảo database được cập nhật trước khi ứng dụng xử lý requests
    public static IServiceCollection AddMigration<TContext>(this IServiceCollection services, Func<TContext, IServiceProvider, Task> seeder)
        where TContext : DbContext
    {
        // Enable migration tracing
        services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(ActivitySourceName));

        return services.AddHostedService(sp => new MigrationHostedService<TContext>(sp, seeder));
    }

    public static IServiceCollection AddMigration<TContext, TDbSeeder>(this IServiceCollection services)
        where TContext : DbContext
        where TDbSeeder : class, IDbSeeder<TContext>
    {
        services.AddScoped<IDbSeeder<TContext>, TDbSeeder>();
        return services.AddMigration<TContext>((context, sp) => sp.GetRequiredService<IDbSeeder<TContext>>().SeedAsync(context));
    }

    private static async Task MigrateDbContextAsync<TContext>(this IServiceProvider services, Func<TContext, IServiceProvider, Task> seeder) where TContext : DbContext
    {
        using var scope = services.CreateScope();
        var scopeServices = scope.ServiceProvider;
        var logger = scopeServices.GetRequiredService<ILogger<TContext>>();
        var context = scopeServices.GetService<TContext>();

        using var activity = ActivitySource.StartActivity($"Migration operation {typeof(TContext).Name}");

        try
        {
            logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);
            // Tạo execution strategy để xử lý các lỗi tạm thời khi thao tác với database
            // Strategy này sẽ:
            // 1. Tự động retry khi gặp lỗi kết nối tạm thời
            // 2. Áp dụng exponential backoff giữa các lần retry
            // 3. Đảm bảo tính nhất quán khi thực hiện transaction
            var strategy = context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(() => InvokeSeeder(seeder, context, scopeServices));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}", typeof(TContext).Name);

            activity.SetExceptionTags(ex);

            throw;
        }
    }

    private static async Task InvokeSeeder<TContext>(Func<TContext, IServiceProvider, Task> seeder, TContext context, IServiceProvider services)
        where TContext : DbContext
    {
        using var activity = ActivitySource.StartActivity($"Migrating {typeof(TContext).Name}");

        try
        {
            await context.Database.MigrateAsync();
            await seeder(context, services);
        }
        catch (Exception ex)
        {
            activity.SetExceptionTags(ex);

            throw;
        }
    }

    private class MigrationHostedService<TContext>(IServiceProvider serviceProvider, Func<TContext, IServiceProvider, Task> seeder)
        : BackgroundService where TContext : DbContext
    {
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return serviceProvider.MigrateDbContextAsync(seeder);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}
// Từ khóa 'in' đánh dấu tham số kiểu TContext là contravariant (đối biến), có ý nghĩa:
// 1. Cho phép truyền vào interface một kiểu TContext hoặc kiểu cơ sở của nó
// 2. TContext chỉ có thể được sử dụng làm tham số đầu vào của phương thức
// 3. Ví dụ: nếu có class MyDbContext : DbContext, thì:
//    - IDbSeeder<DbContext> seeder = new MySeeder(); // Hợp lệ  
//    - IDbSeeder<MyDbContext> seeder = new DbSeeder(); // Không hợp lệ
// 4. Điều này giúp code linh hoạt hơn nhưng vẫn đảm bảo type safety
public interface IDbSeeder<in TContext> where TContext : DbContext
{
    Task SeedAsync(TContext context);
}

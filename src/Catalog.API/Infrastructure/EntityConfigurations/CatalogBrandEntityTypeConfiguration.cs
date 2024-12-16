// Namespace chứa các cấu hình Entity Framework cho các entity trong Catalog API
namespace eShop.Catalog.API.Infrastructure.EntityConfigurations;

// Lớp cấu hình cho entity CatalogBrand
// Implements interface IEntityTypeConfiguration để định nghĩa cách map entity với database
class CatalogBrandEntityTypeConfiguration
    // IEntityTypeConfiguration là interface của EF Core dùng để:
    // 1. Tách biệt logic cấu hình entity ra khỏi entity model
    // 2. Áp dụng nguyên tắc Single Responsibility - mỗi class chỉ có một nhiệm vụ
    // 3. Dễ dàng tái sử dụng cấu hình cho nhiều DbContext khác nhau
    // 4. Code gọn gàng và dễ maintain hơn so với cấu hình trực tiếp trong OnModelCreating
    : IEntityTypeConfiguration<CatalogBrand>
{
    // Phương thức Configure được yêu cầu bởi interface
    // Dùng để cấu hình chi tiết cách map entity CatalogBrand với database
    public void Configure(EntityTypeBuilder<CatalogBrand> builder)
    {
        // Chỉ định tên bảng trong database là "CatalogBrand"
        builder.ToTable("CatalogBrand");

        // Cấu hình cho thuộc tính Brand:
        // - Giới hạn độ dài tối đa là 100 ký tự
        // cb là tham số lambda đại diện cho CatalogBrand
        builder.Property(cb => cb.Brand)
            .HasMaxLength(100);
    }
}

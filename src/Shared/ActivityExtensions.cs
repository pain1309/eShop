using System.Diagnostics;

internal static class ActivityExtensions
{
    // Phương thức mở rộng này được sử dụng để thêm thông tin về exception vào Activity trong OpenTelemetry
    // Mục đích:
    // 1. Tuân thủ quy ước semantic conventions của OpenTelemetry về exception tracking
    // 2. Giúp theo dõi và debug lỗi dễ dàng hơn bằng cách:
    //    - Lưu message của exception
    //    - Lưu stack trace đầy đủ 
    //    - Lưu loại exception
    //    - Đánh dấu activity là có lỗi
    // 3. Cung cấp API đơn giản để gắn thông tin exception vào activity hiện tại
    public static void SetExceptionTags(this Activity activity, Exception ex)
    {
        if (activity is null)
        {
            return;
        }

        activity.AddTag("exception.message", ex.Message);
        activity.AddTag("exception.stacktrace", ex.ToString());
        activity.AddTag("exception.type", ex.GetType().FullName);
        activity.SetStatus(ActivityStatusCode.Error);
    }
}

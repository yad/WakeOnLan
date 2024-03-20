public static class VisitServiceExt
{
    public static void UseVisitor(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            VisitService? visitService = context.RequestServices.GetService<VisitService>();
            if (visitService != null)
            {
                visitService.LastVisitDate = DateTime.Now;
            }

            await next.Invoke();
        });
    }
}

public class VisitService
{
    public DateTime LastVisitDate { get; set; } = DateTime.MinValue;

    public bool IsActive => LastVisitDate.AddMinutes(1) > DateTime.Now;
}

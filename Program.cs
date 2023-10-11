using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Net;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddApplicationInsightsTelemetryProcessor<SuccessfulDependencyFilter>();
            //builder.Services.AddApplicationInsightsTelemetryProcessor<AnotherProcess>();


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }

        public class SuccessfulDependencyFilter : ITelemetryProcessor
        {
            private ITelemetryProcessor Next { get; set; }

            // next will point to the next TelemetryProcessor in the chain.
            public SuccessfulDependencyFilter(ITelemetryProcessor next)
            {
                this.Next = next;
            }

            public void Process(ITelemetry item)
            {
                // To filter out an item, return without calling the next processor.
                if (!OKtoSend(item)) 
                { 
                    return; 
                }

                this.Next.Process(item);
            }

            // Example: replace with your own criteria.
            private bool OKtoSend(ITelemetry item)
            {
                var dependency = item as DependencyTelemetry;
                if (dependency == null) return true;

                if (item is RequestTelemetry requestTelemetry)
                {
                    if (requestTelemetry.ResponseCode == "404"  && requestTelemetry.Success == true)
                    {
                        if (requestTelemetry.Properties.TryGetValue("httpResponseSubStatus", out var subStatus))
                        {
                            if (subStatus == "11")
                            {
                                return false;
                            }
                        }
                    }
                }

                return dependency.Success != true;
            }
        }
    }
}
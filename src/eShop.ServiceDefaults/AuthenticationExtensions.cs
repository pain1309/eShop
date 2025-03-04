﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

namespace eShop.ServiceDefaults;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddDefaultAuthentication(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // {
        //   "Identity": {
        //     "Url": "http://identity",
        //     "Audience": "basket"
        //    }
        // }

        var identitySection = configuration.GetSection("Identity");

        if (!identitySection.Exists())
        {
            // No identity section, so no authentication
            return services;
        }

        // prevent from mapping "sub" claim to nameidentifier.
        // Trong JWT, claim "sub" thường được sử dụng để đại diện cho "subject" của token, tức là người dùng hoặc thực thể mà token đó đại diện.
        // Mặc định, một số thư viện xác thực có thể ánh xạ claim này thành "nameidentifier",
        // điều này có thể không mong muốn trong một số trường hợp, đặc biệt là khi bạn muốn giữ nguyên giá trị của claim "sub" cho các mục đích khác.
        // Việc loại bỏ ánh xạ này giúp bạn kiểm soát tốt hơn cách mà các claim được xử lý và sử dụng trong ứng dụng của bạn.
        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

        services.AddAuthentication().AddJwtBearer(options =>
        {
            var identityUrl = identitySection.GetRequiredValue("Url");
            var audience = identitySection.GetRequiredValue("Audience");

            options.Authority = identityUrl;
            options.RequireHttpsMetadata = false;
            options.Audience = audience;
            
#if DEBUG
            //Needed if using Android Emulator Locally. See https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-8.0#android
            options.TokenValidationParameters.ValidIssuers = [identityUrl, "https://10.0.2.2:5243"];
#else
            options.TokenValidationParameters.ValidIssuers = [identityUrl];
#endif
            
            options.TokenValidationParameters.ValidateAudience = false;
        });

        services.AddAuthorization();

        return services;
    }
}

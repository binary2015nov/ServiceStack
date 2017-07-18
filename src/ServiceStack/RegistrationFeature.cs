using System;
using ServiceStack.Auth;
using ServiceStack.FluentValidation;

namespace ServiceStack
{
    /// <summary>
    /// Enable the Registration feature and configure the RegistrationService.
    /// </summary>
    public class RegistrationFeature : IPlugin, IPostInitPlugin
    {
        public string AtRestPath { get; set; }

        public RegistrationFeature()
        {
            this.AtRestPath = "/register";
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterService<RegisterService>(AtRestPath);
            appHost.RegisterAs<RegistrationValidator, IValidator<Register>>();
        }

        public void AfterPluginsLoaded(IAppHost appHost)
        {
            var authRepository = appHost.TryResolve<RegisterService>().AuthRepository as IUserAuthRepository;
            if (authRepository == null)
                throw new Exception("There is no user auth repository in register service.");
        }
    }
}
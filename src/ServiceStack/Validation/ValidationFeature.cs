using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Funq;
using ServiceStack.FluentValidation;
using ServiceStack.FluentValidation.Results;
using ServiceStack.Text;

namespace ServiceStack.Validation
{
    public class ValidationFeature : IPlugin
    {
        public Func<ValidationResult, object, object> ErrorResponseFilter { get; set; }

        public bool ScanAppHostAssemblies { get; set; } = true;

        /// <summary>
        /// Activate the validation mechanism, so every request DTO with an existing validator
        /// will be validated.
        /// </summary>
        /// <param name="appHost">The app host</param>
        public void Register(IAppHost appHost)
        {
            if (!appHost.GlobalRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync))
            {
                appHost.GlobalRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsync);
            }

            if (!appHost.GlobalMessageRequestFiltersAsync.Contains(ValidationFilters.RequestFilterAsync))
            {
                appHost.GlobalMessageRequestFiltersAsync.Add(ValidationFilters.RequestFilterAsync);
            }

            if (ScanAppHostAssemblies)
            {
                appHost.GetContainer().RegisterValidators(appHost.ServiceAssemblies);
            }

            appHost.GetPlugin<MetadataFeature>()
                ?.AddLink(MetadataFeature.AvailableFeatures, "http://docs.servicestack.net/validation", "validation");
        }
       
        /// <summary>
        /// Override to provide additional/less context about the Service Exception. 
        /// By default the request is serialized and appended to the ResponseStatus StackTrace.
        /// </summary>
        public virtual string GetRequestErrorBody(object request)
        {
            var requestString = "";
            try
            {
                requestString = TypeSerializer.SerializeToString(request);
            }
            catch /*(Exception ignoreSerializationException)*/
            {
                //Serializing request successfully is not critical and only provides added error info
            }

            return $"[{GetType().GetOperationName()}: {DateTime.Now}]:\n[REQUEST: {requestString}]";
        }
    }

    public static class ValidationExtensions
    {
        /// <summary>
        /// Auto-scans the provided assemblies for a <see cref="IValidator"/>
        /// and registers it in the provided IoC container.
        /// </summary>
        /// <param name="container">The IoC container</param>
        /// <param name="assemblies">The assemblies to scan for a validator</param>
        public static void RegisterValidators(this Container container, params Assembly[] assemblies)
        {
            RegisterValidators(container, ReuseScope.None, assemblies);
        }

        public static void RegisterValidators(this Container container, ReuseScope scope, params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                foreach (var validator in assembly.GetTypes()
                .Where(t => t.IsOrHasGenericInterfaceTypeOf(typeof(IValidator<>))))
                {
                    RegisterValidator(container, validator, scope);
                }
            }
        }

        public static void RegisterValidator(this Container container, Type validator, ReuseScope scope = ReuseScope.None)
        {
            if (validator.IsInterface)
                return;

            var type = validator;
            while (!type.IsGenericType && type.BaseType != null)
            {
                type = type.BaseType;
            }

            var dtoType = type.GetGenericArguments()[0];
            var validatorType = typeof(IValidator<>).GetCachedGenericType(dtoType);

            container.RegisterAutoWiredType(validator, validatorType, scope);
        }

        public static bool HasAsyncValidators(this IValidator validator, string ruleSet = null)
        {
            if (validator is IEnumerable<IValidationRule> rules)
            {
                foreach (var rule in rules)
                {
                    if (ruleSet != null && rule.RuleSet != null && rule.RuleSet != ruleSet)
                        continue;

                    if (rule.Validators.Any(x => x.IsAsync))
                        return true;
                }
            }
            return false;
        }
    }
}

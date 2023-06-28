using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Neon.Operator.Attributes;

namespace Neon.Operator.Core
{
    internal class AssemblyScanner
    {
        public HashSet<Type> EntityTypes { get; set;}

        public AssemblyScanner()
        {
            this.EntityTypes = new HashSet<Type>();
        }

        public void Add(Assembly assembly)
        {
            Scan(assembly);
        }

        public void Add(Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                Scan(assembly);
            }
        }

        public void Add(string assemblyPath)
        {
            Scan(Assembly.LoadFrom(assemblyPath));
        }

        private void Scan(Assembly assembly)
        {
            List<Type> assemblyTypes = new List<Type>();

            try
            {
                assemblyTypes = assembly.GetTypes().Where(type => type != null).ToList();
            }
            catch (ReflectionTypeLoadException e)
            {
                assemblyTypes = e.Types.Where(type => type != null).ToList();
            }

            var types = assemblyTypes
                .Where(type => type.GetInterfaces().Count() > 0
                        && type.GetInterfaces().Any(@interface => @interface.GetCustomAttributes<OperatorComponentAttribute>()
                    .Any())).ToList();

            foreach (var type in types)
            {
                switch (type.GetInterfaces()
                    .Where(@interface => @interface.GetCustomAttributes<OperatorComponentAttribute>()
                    .Any())
                    .Select(@interface => @interface.GetCustomAttribute<OperatorComponentAttribute>())
                    .FirstOrDefault().ComponentType)
                {
                    case OperatorComponentType.Controller:

                        if (type.GetCustomAttribute<ResourceControllerAttribute>()?.Ignore == true
                                || type.Name == "ResourceControllerBase")
                        {
                            break;
                        }

                        var entityTypes = type.GetInterfaces()
                            .Where(@interface => @interface.IsConstructedGenericType 
                            && @interface.GetGenericTypeDefinition().Name == "IResourceController`1")
                            .Select(@interface => @interface.GenericTypeArguments[0]);


                        foreach (var entityType in entityTypes)
                        {
                            //ComponentRegister.RegisterController(type, entityType);
                            EntityTypes.Add(entityType);
                        }

                        break;

                    case OperatorComponentType.Finalizer:

                        if (type.GetCustomAttribute<ResourceFinalizerAttribute>()?.Ignore == true)
                        {
                            break;
                        }

                        var finalizerEntityTypes = type.GetInterfaces()
                            .Where(@interface => @interface.IsConstructedGenericType
                            && @interface.GetGenericTypeDefinition().Name == "IResourceFinalizer`1")
                            .Select(@interface => @interface.GenericTypeArguments[0]);

                        foreach (var entityType in finalizerEntityTypes)
                        {
                            //ComponentRegister.RegisterFinalizer(type, entityType);
                            EntityTypes.Add(entityType);
                        }

                        break;

                    case OperatorComponentType.MutationWebhook:

                        if (type.GetCustomAttribute<MutatingWebhookAttribute>()?.Ignore == true)
                        {
                            break;
                        }

                        var mutatingWebhookEntityTypes = type.GetInterfaces()
                            .Where(@interface => @interface.IsConstructedGenericType
                            && @interface.GetGenericTypeDefinition().Name == "IMutatingWebhook`1")
                            .Select(@interface => @interface.GenericTypeArguments[0]);

                        foreach (var entityType in mutatingWebhookEntityTypes)
                        {
                            //ComponentRegister.RegisterMutatingWebhook(type, entityType);
                            EntityTypes.Add(entityType);
                        }

                        break;

                    case OperatorComponentType.ValidationWebhook:

                        if (type.GetCustomAttribute<ValidatingWebhookAttribute>()?.Ignore == true)
                        {
                            break;
                        }

                        var validatingWebhookEntityTypes = type.GetInterfaces()
                            .Where(@interface => @interface.IsConstructedGenericType
                            && @interface.GetGenericTypeDefinition().Name == "IValidatingWebhook`1")
                            .Select(@interface => @interface.GenericTypeArguments[0]);

                        foreach (var entityType in validatingWebhookEntityTypes)
                        {
                            //ComponentRegister.RegisterValidatingWebhook(type, entityType);
                            EntityTypes.Add(entityType);
                        }

                        break;
                }
            }
        }
    }
}

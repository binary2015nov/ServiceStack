using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Funq
{
	public partial class Container
    {
        /* The following regions contain just the typed overloads
		 * that are just pass-through to the real implementations.
		 * They all have DebuggerStepThrough to ease debugging. */

        #region Register

        /// <summary>
        /// Registers the given service by providing a factory delegate to instantiate it.
		/// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService>(Func<Container, TService> factory)
		{
			return Register(null, factory);
		}

        /// <summary>
        /// Registers the given service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <typeparam name="TArg">First argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg>(Func<Container, TArg, TService> factory)
		{
			return Register(null, factory);
		}

        /// <summary>
        /// Registers the given service by providing a factory delegate that receives arguments to instantiate it.
		/// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <typeparam name= "TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2>(Func<Container, TArg1, TArg2, TService> factory)
		{
			return Register(null, factory);
		}

        /// <summary>
        /// Registers the given service by providing a factory delegate that receives arguments to instantiate it.
		/// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>
		[DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3>(Func<Container, TArg1, TArg2, TArg3, TService> factory)
		{
			return Register(null, factory);
        }

	    /// <summary>
        /// Registers the given service by providing a factory delegate that receives arguments to instantiate it.
		/// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <typeparam name="TArg4">Fourth argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4>(Func<Container, TArg1, TArg2, TArg3, TArg4, TService> factory)
		{
			return Register(null, factory);
		}

        /// <summary>
        /// Registers the given service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService> factory)
		{
			return Register(null, factory);
		}

        /// <summary>
        /// Registers the given service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg6">Sixth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> factory)
		{
			return Register(null, factory);
        }

		/// <summary>
		///	Registers the given named service by providing a factory delegate to instantiate it.
		/// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <param name="name">A name used to differenciate this service registration.</param>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>		
        [DebuggerStepThrough]
		public IRegistration<TService> Register<TService>(string name, Func<Container, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TService>>(name, factory);
		}

        /// <summary>
        /// Registers the given named service by providing a factory delegate that receives arguments to instantiate it.
		/// </summary>
		/// <typeparam name="TService">The service type to register.</typeparam>
		/// <typeparam name="TArg">First argument that should be passed to the factory delegate to create the instace.</typeparam>
		/// <param name="name">A name used to differenciate this service registration.</param>
		/// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
		/// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
		public IRegistration<TService> Register<TService, TArg>(string name, Func<Container, TArg, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg, TService>>(name, factory);
		}

        /// <summary>
        /// Registers the given named service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="name">A name used to differenciate this service registration.</param>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2>(string name, Func<Container, TArg1, TArg2, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TService>>(name, factory);
		}

        /// <summary>
        /// Registers the given named service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="name">A name used to differenciate this service registration.</param>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3>(string name, Func<Container, TArg1, TArg2, TArg3, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TService>>(name, factory);
		}

        /// <summary>
        /// Registers the given named service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Fouth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="name">A name used to differenciate this service registration.</param>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4>(string name, Func<Container, TArg1, TArg2, TArg3, TArg4, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TService>>(name, factory);
		}

        /// <summary>
        /// Registers the given named service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Fouth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="name">A name used to differenciate this service registration.</param>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService>>(name, factory);
		}

        /// <summary>
        /// Registers the given named service by providing a factory delegate that receives arguments to instantiate it.
        /// </summary>
        /// <typeparam name="TService">The service type to register.</typeparam>
        /// <typeparam name="TArg1">First argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Fouth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg6">Sixth argument that should be passed to the factory delegate to create the instace.</typeparam>
        /// <param name="name">A name used to differenciate this service registration.</param>
        /// <param name="factory">The factory delegate to initialize new instances of the service when needed.</param>
        /// <returns>The registration object to perform further configuration via its fluent interface.</returns>
        [DebuggerStepThrough]
        public IRegistration<TService> Register<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> factory)
		{
			return RegisterImpl<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService>>(name, factory);
        }

		#endregion

		#region Resolve

		/// <summary>
        /// Resolves the given service by type, without passing any arguments for its construction.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <returns>The resolved service instance.</returns>
		/// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
		public TService Resolve<TService>()
		{
			return ResolveNamed<TService>(null);
        }

		/// <summary>
        /// Resolves the given service by type, passing the given arguments for its initialization.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <typeparam name="TArg">First argument to pass to the factory delegate that may create the instace.</typeparam>
		/// <returns>The resolved service instance.</returns>
		/// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
		public TService Resolve<TService, TArg>(TArg arg)
		{
			return ResolveNamed<TService, TArg>(null, arg);
		}

        /// <summary>
        /// Resolves the given service by type, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService Resolve<TService, TArg1, TArg2>(TArg1 arg1, TArg2 arg2)
		{
			return ResolveNamed<TService, TArg1, TArg2>(null, arg1, arg2);
		}

        /// <summary>
        /// Resolves the given service by type, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService Resolve<TService, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3>(null, arg1, arg2, arg3);
		}

        /// <summary>
        /// Resolves the given service by type, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService Resolve<TService, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(null, arg1, arg2, arg3, arg4);
		}

        /// <summary>
        /// Resolves the given service by type, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService Resolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(null, arg1, arg2, arg3, arg4, arg5);
		}

        /// <summary>
        /// Resolves the given service by type, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg6">Sixth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService Resolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(null, arg1, arg2, arg3, arg4, arg5, arg6);
        }

		#endregion

		#region ResolveNamed

		/// <summary>
        /// Resolves the given service by type and name, without passing arguments for its initialization.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <returns>The resolved service instance.</returns>
		/// <exception cref="ResolutionException">The given service could not be resolved.</exception>		
        [DebuggerStepThrough]
		public TService ResolveNamed<TService>(string name)
		{
			return ResolveImpl<TService>(name, true);
        }

		/// <summary>
        /// Resolves the given service by type and name, passing the given arguments for its initialization.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <typeparam name="TArg">First argument to pass to the factory delegate that may create the instace.</typeparam>
		/// <returns>The resolved service instance.</returns>
		/// <exception cref="ResolutionException">The given service could not be resolved.</exception>		
        [DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg>(string name, TArg arg)
		{
			return ResolveImpl<TService, TArg>(name, true, arg);
		}

        /// <summary>
        /// Resolves the given service by type and name, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService ResolveNamed<TService, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2)
		{
			return ResolveImpl<TService, TArg1, TArg2>(name, true, arg1, arg2);
		}

        /// <summary>
        /// Resolves the given service by type and name, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService ResolveNamed<TService, TArg1, TArg2, TArg3>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3>(name, true, arg1, arg2, arg3);
		}

        /// <summary>
        /// Resolves the given service by type and name, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4>(name, true, arg1, arg2, arg3, arg4);
		}

        /// <summary>
        /// Resolves the given service by type and name, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
		public TService ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(name, true, arg1, arg2, arg3, arg4, arg5);
		}

        /// <summary>
        /// Resolves the given service by type and name, passing the given arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg6">Sixth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance.</returns>
        /// <exception cref="ResolutionException">The given service could not be resolved.</exception>
        [DebuggerStepThrough]
        public TService ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(name, true, arg1, arg2, arg3, arg4, arg5, arg6);
        }

		#endregion

		#region TryResolve

		/// <summary>
        /// Attempts to resolve the given service by type, without passing arguments for its initialization.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolve<TService>()
		{
			return TryResolveNamed<TService>(null);
        }

		/// <summary>
        /// Attempts to resolve the given service by type, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <typeparam name="TArg">First argument to pass to the factory delegate that may create the instace.</typeparam>
		/// <returns>The resolved service instance or<see langword="null"/> if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolve<TService, TArg>(TArg arg)
		{
			return TryResolveNamed<TService, TArg>(null, arg);
		}

        /// <summary>
        /// Attempts to resolve the given service by type, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
        public TService TryResolve<TService, TArg1, TArg2>(TArg1 arg1, TArg2 arg2)
		{
			return TryResolveNamed<TService, TArg1, TArg2>(null, arg1, arg2);
		}

        /// <summary>
        /// Attempts to resolve the given service by type, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3>(null, arg1, arg2, arg3);
		}

        /// <summary>
        /// Attempts to resolve the given service by type, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(null, arg1, arg2, arg3, arg4);
		}

        /// <summary>
        /// Attempts to resolve the given service by type, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
        public TService TryResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(null, arg1, arg2, arg3, arg4, arg5);
		}

        /// <summary>
        /// Attempts to resolve the given service by type, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg6">Sixth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(null, arg1, arg2, arg3, arg4, arg5, arg6);
        }

		#endregion

		#region TryResolveNamed

		/// <summary>
        /// Attempts to resolve the given service by type and name, without passing arguments arguments for its initialization.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolveNamed<TService>(string name)
		{
			return ResolveImpl<TService>(name, false);
        }

        /// <summary>
		///	Attempts to resolve the given service by type and name, passing the given arguments arguments for its initialization.
        /// </summary>
		/// <typeparam name="TService">Type of the service to retrieve.</typeparam>
		/// <typeparam name="TArg">First argument to pass to the factory delegate that may create the instace.</typeparam>
		/// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg>(string name, TArg arg)
		{
			return ResolveImpl<TService, TArg>(name, false, arg);
		}

        /// <summary>
        ///	Attempts to resolve the given service by type and name, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2>(string name, TArg1 arg1, TArg2 arg2)
		{
			return ResolveImpl<TService, TArg1, TArg2>(name, false, arg1, arg2);
		}

        /// <summary>
        ///	Attempts to resolve the given service by type and name, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
		public TService TryResolveNamed<TService, TArg1, TArg2, TArg3>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3>(name, false, arg1, arg2, arg3);
		}

        /// <summary>
        ///	Attempts to resolve the given service by type and name, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
        public TService TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4>(name, false, arg1, arg2, arg3, arg4);
		}

        /// <summary>
        ///	Attempts to resolve the given service by type and name, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
        public TService TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(name, false, arg1, arg2, arg3, arg4, arg5);
		}

        /// <summary>
        ///	Attempts to resolve the given service by type and name, passing the given arguments arguments for its initialization.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">First argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg2">Second argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg3">Third argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg4">Fourth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg5">Fifth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <typeparam name="TArg6">Sixth argument to pass to the factory delegate that may create the instace.</typeparam>
        /// <returns>The resolved service instance or null if it cannot be resolved.</returns>
        [DebuggerStepThrough]
        public TService TryResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
		{
			return ResolveImpl<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(name, false, arg1, arg2, arg3, arg4, arg5, arg6);
		}

        #endregion

        #region LazyResolve

        /// <summary>
        /// Retrieves a function that can be used to lazily resolve an instance of the service of the given type when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <returns>The function that can resolve to the service instance when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TService> LazyResolve<TService>()
        {
            return LazyResolve<TService>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <returns>The function that can resolve to the service instance with the given constructor arguments when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg, TService> LazyResolve<TService, TArg>()
        {
            return LazyResolve<TService, TArg>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <returns>The function that can resolve to the service instance with the given constructor arguments when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TService> LazyResolve<TService, TArg1, TArg2>()
        {
            return LazyResolve<TService, TArg1, TArg2>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <returns>The function that can resolve to the service instance with the given constructor arguments when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TService> LazyResolve<TService, TArg1, TArg2, TArg3>()
        {
            return LazyResolve<TService, TArg1, TArg2, TArg3>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Type of the four argument to pass to the factory delegate to create the instace.</typeparam>
        /// <returns>The function that can resolve to the service instance with the given constructor arguments when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TArg4, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4>()
        {
            return LazyResolve<TService, TArg1, TArg2, TArg3, TArg4>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Type of the four argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Type of the five argument to pass to the factory delegate to create the instace.</typeparam>
        /// <returns>The function that can resolve to the service instance with the given constructor arguments when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>()
        {
            return LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Type of the four argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Type of the five argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg6">Type of the six argument to pass to the factory delegate to create the instace.</typeparam>
        /// <returns>The function that can resolve to the service instance with the given constructor arguments when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>()
        {
            return LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(null);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service with the given name when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TService> LazyResolve<TService>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TService>>(name);
            return () => ResolveNamed<TService>(name);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type, name and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given and service constructor arguments name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name and constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg, TService> LazyResolve<TService, TArg>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TArg, TService>>(name);
            return arg => ResolveNamed<TService, TArg>(name, arg);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type, name and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given and service constructor arguments name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name and constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TService> LazyResolve<TService, TArg1, TArg2>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TService>>(name);
            return (arg1, arg2) => ResolveNamed<TService, TArg1, TArg2>(name, arg1, arg2);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type, name and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given and service constructor arguments name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name and constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TService> LazyResolve<TService, TArg1, TArg2, TArg3>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TService>>(name);
            return (arg1, arg2, arg3) => ResolveNamed<TService, TArg1, TArg2, TArg3>(name, arg1, arg2, arg3);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type, name and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the four argument to pass to the factory delegate to create the instace.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given and service constructor arguments name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name and constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TArg4, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TService>>(name);
            return (arg1, arg2, arg3, arg4) => ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4>(name, arg1, arg2, arg3, arg4);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type, name and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Type of the four argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Type of the five argument to pass to the factory delegate to create the instace.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given and service constructor arguments name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name and constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TService>>(name);
            return (arg1, arg2, arg3, arg4, arg5) => ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5>(name, arg1, arg2, arg3, arg4, arg5);
        }

        /// <summary>
        ///	Retrieves a function that can be used to lazily resolve an instance of the service of the given type, name and service constructor arguments when needed.
        /// </summary>
        /// <typeparam name="TService">Type of the service to retrieve.</typeparam>
        /// <typeparam name="TArg1">Type of the one argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg2">Type of the two argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg3">Type of the three argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg4">Type of the four argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg5">Type of the five argument to pass to the factory delegate to create the instace.</typeparam>
        /// <typeparam name="TArg6">Type of the six argument to pass to the factory delegate to create the instace.</typeparam>
        /// <param name="name">Name of the service to retrieve.</param>
        /// <returns>The function that can resolve to the service instance with the given and service constructor arguments name when invoked.</returns>
        /// <exception cref="ResolutionException">The requested service with the given name and constructor arguments has not been registered previously.</exception>
        [DebuggerStepThrough]
        public Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService> LazyResolve<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(string name)
        {
            ThrowIfNotRegistered<TService, Func<Container, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TService>>(name);
            return (arg1, arg2, arg3, arg4, arg5, arg6) => ResolveNamed<TService, TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(name, arg1, arg2, arg3, arg4, arg5, arg6);
        }

        #endregion
    }
}

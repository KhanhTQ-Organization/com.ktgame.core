using System;
using System.Collections.Generic;

namespace com.ktgame.core.di
{
	public class Injector : IInjector
	{
		private readonly DisposableCollection _disposables = new DisposableCollection();
		private readonly ConstructorInjector _constructorInjector;
		private readonly AttributeInjector _attributeInjector;

		public Dictionary<Type, IResolver> ResolversByContract { get; } = new Dictionary<Type, IResolver>();

		public Injector()
		{
			_constructorInjector = new ConstructorInjector(this);
			_attributeInjector = new AttributeInjector(this);
			Diagnosis.RegisterBuildCallSite(this);
		}

		public void Dispose()
		{
			_disposables?.Dispose();
			ResolversByContract.Clear();
		}

		public void Resolve(object target)
		{
			if (target != null)
			{
				_attributeInjector.Inject(target);
			}
		}

		public object GetConcreteByContract(Type contract)
		{
			return ResolversByContract.TryGetValue(contract, out var resolver) ? resolver.Resolve(this) : null;
		}

		public TConcrete Construct<TConcrete>(TConcrete concrete)
		{
			var instance = _constructorInjector.Construct(concrete.GetType());
			_attributeInjector.Inject(instance);
			return (TConcrete)instance;
		}

		public void AddSingleton(Type concrete, Type contract)
		{
			Add(concrete, contract, new SingletonTypeResolver(concrete));
		}

		public void AddSingleton(object instance, Type contract)
		{
			Add(instance.GetType(), contract, new SingletonValueResolver(instance));
		}

		public void AddSingleton<T>(Func<Injector, T> factory, Type contract)
		{
			var resolver = new SingletonFactoryResolver(Proxy);
			Add(typeof(T), contract, resolver);
			return;

			object Proxy(Injector injector)
			{
				return factory.Invoke(injector);
			}
		}

		public void AddTransient(Type concrete, Type contract)
		{
			Add(concrete, contract, new TransientTypeResolver(concrete));
		}

		public void AddTransient(object instance, Type contract)
		{
			Add(instance.GetType(), contract, new TransientValueResolver(instance));
		}

		public void AddTransient<T>(Func<Injector, T> factory, Type contract)
		{
			var resolver = new TransientFactoryResolver(Proxy);
			Add(typeof(T), contract, resolver);
			return;

			object Proxy(Injector injector)
			{
				return factory.Invoke(injector);
			}
		}

		public bool HasBinding(Type type)
		{
			return ResolversByContract.ContainsKey(type);
		}

		public void Remove(Type contract)
		{
			if (ResolversByContract.TryGetValue(contract, out var resolver))
			{
				_disposables.Remove(resolver);
				ResolversByContract.Remove(contract);
			}
		}

		private void Add(Type concrete, Type contract, IResolver resolver)
		{
			if (ValidateContracts(concrete, contract))
			{
				if (ResolversByContract.ContainsKey(contract))
				{
					UnityEngine.Debug.LogWarning($"[Injector] Contract '{contract.GetFullName()}' is already bound. New binding for '{concrete.GetFullName()}' was ignored.");
					return;
				}

				_disposables.Add(resolver);
				ResolversByContract.Add(contract, resolver);
			}
		}

		private static bool ValidateContracts(Type concrete, Type contract)
		{
			if (!contract.IsAssignableFrom(concrete))
			{
				throw new ContractDefinitionException(concrete, contract);
			}

			return true;
		}
	}
}

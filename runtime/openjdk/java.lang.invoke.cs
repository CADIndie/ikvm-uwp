/*
  Copyright (C) 2011-2014 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;
using System.Diagnostics;
using System.Reflection;
#if WINRT
using WinRT.Reflection.Stub;
#else
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;
using IKVM.Internal;
using java.lang.invoke;
using jlClass = java.lang.Class;

static class Java_java_lang_invoke_MethodHandle
{
	public static object invokeExact(MethodHandle thisObject, object[] args)
	{
#if FIRST_PASS
		return null;
#else
		return IKVM.Runtime.ByteCodeHelper.GetDelegateForInvokeExact<IKVM.Runtime.MH<object[], object>>(thisObject)(args);
#endif
	}

	public static object invoke(MethodHandle thisObject, object[] args)
	{
#if FIRST_PASS
		return null;
#else
		return thisObject.invokeWithArguments(args);
#endif
	}

	public static object invokeBasic(MethodHandle thisObject, object[] args)
	{
		throw new InvalidOperationException();
	}

	public static object linkToVirtual(object[] args)
	{
		throw new InvalidOperationException();
	}

	public static object linkToStatic(object[] args)
	{
		throw new InvalidOperationException();
	}

	public static object linkToSpecial(object[] args)
	{
		throw new InvalidOperationException();
	}

	public static object linkToInterface(object[] args)
	{
		throw new InvalidOperationException();
	}
}

static class Java_java_lang_invoke_MethodHandleNatives
{
	// called from Lookup.revealDirect() (instead of MethodHandle.internalMemberName()) via map.xml replace-method-call
	public static MemberName internalMemberName(MethodHandle mh)
	{
#if FIRST_PASS
		return null;
#else
		MemberName mn = mh.internalMemberName();
		if (mn.isStatic() && mn.getName() == "<init>")
		{
			// HACK since we convert String constructors into static methods, we have to undo that here
			// Note that the MemberName we return is only used for a security check and by InfoFromMemberName (a MethodHandleInfo implementation),
			// so we don't need to make it actually invokable.
			MemberName alt = new MemberName();
			typeof(MemberName).GetField("clazz", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(alt, mn.getDeclaringClass());
			typeof(MemberName).GetField("name", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(alt, mn.getName());
			typeof(MemberName).GetField("type", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(alt, mn.getMethodType().changeReturnType(typeof(void)));
			int flags = mn._flags();
			flags -= MethodHandleNatives.Constants.MN_IS_METHOD;
			flags += MethodHandleNatives.Constants.MN_IS_CONSTRUCTOR;
			flags &= ~(MethodHandleNatives.Constants.MN_REFERENCE_KIND_MASK << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT);
			flags |= MethodHandleNatives.Constants.REF_newInvokeSpecial << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags &= ~MethodHandleNatives.Constants.ACC_STATIC;
			alt._flags(flags);
			return alt;
		}
		return mn;
#endif
	}

	public static void init(MemberName self, object refObj)
	{
		init(self, refObj, false);
	}

	// this overload is called via a map.xml patch to the MemberName(Method, boolean) constructor, because we need wantSpecial
	public static void init(MemberName self, object refObj, bool wantSpecial)
	{
#if !FIRST_PASS
		java.lang.reflect.Method method;
		java.lang.reflect.Constructor constructor;
		java.lang.reflect.Field field;
		if ((method = refObj as java.lang.reflect.Method) != null)
		{
			InitMethodImpl(self, MethodWrapper.FromExecutable(method), wantSpecial);
		}
		else if ((constructor = refObj as java.lang.reflect.Constructor) != null)
		{
			InitMethodImpl(self, MethodWrapper.FromExecutable(constructor), wantSpecial);
		}
		else if ((field = refObj as java.lang.reflect.Field) != null)
		{
			FieldWrapper fw = FieldWrapper.FromField(field);
			self._clazz(fw.DeclaringType.ClassObject);
			int flags = (int)fw.Modifiers | MethodHandleNatives.Constants.MN_IS_FIELD;
			flags |= (fw.IsStatic ? MethodHandleNatives.Constants.REF_getStatic : MethodHandleNatives.Constants.REF_getField) << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			self._flags(flags);
		}
		else
		{
			throw new InvalidOperationException();
		}
#endif
	}

	private static void InitMethodImpl(MemberName self, MethodWrapper mw, bool wantSpecial)
	{
#if !FIRST_PASS
		int flags = (int)mw.Modifiers;
		flags |= mw.IsConstructor ? MethodHandleNatives.Constants.MN_IS_CONSTRUCTOR : MethodHandleNatives.Constants.MN_IS_METHOD;
		if (mw.IsStatic)
		{
			flags |= MethodHandleNatives.Constants.REF_invokeStatic << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
		}
		else if (mw.IsPrivate || mw.IsFinal || mw.IsConstructor || wantSpecial)
		{
			flags |= MethodHandleNatives.Constants.REF_invokeSpecial << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
		}
		else if (mw.DeclaringType.IsInterface)
		{
			flags |= MethodHandleNatives.Constants.REF_invokeInterface << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
		}
		else
		{
			flags |= MethodHandleNatives.Constants.REF_invokeVirtual << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
		}
		if (mw.HasCallerID || DynamicTypeWrapper.RequiresDynamicReflectionCallerClass(mw.DeclaringType.Name, mw.Name, mw.Signature))
		{
			flags |= MemberName.CALLER_SENSITIVE;
		}
		if (mw.IsConstructor && mw.DeclaringType == CoreClasses.java.lang.String.Wrapper)
		{
			java.lang.Class[] parameters1 = new java.lang.Class[mw.GetParameters().Length];
			for (int i = 0; i < mw.GetParameters().Length; i++)
			{
				parameters1[i] = mw.GetParameters()[i].ClassObject;
			}
			MethodType mt = MethodType.methodType(typeof(string), parameters1);
			typeof(MemberName).GetField("type", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(self, mt);
			self.vmtarget = CreateMemberNameDelegate(mw, null, false, mt);
			flags -= MethodHandleNatives.Constants.REF_invokeSpecial << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags += MethodHandleNatives.Constants.REF_invokeStatic << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags -= MethodHandleNatives.Constants.MN_IS_CONSTRUCTOR;
			flags += MethodHandleNatives.Constants.MN_IS_METHOD;
			flags += MethodHandleNatives.Constants.ACC_STATIC;
			self._flags(flags);
			self._clazz(mw.DeclaringType.ClassObject);
			return;
		}
		self._flags(flags);
		self._clazz(mw.DeclaringType.ClassObject);
		int firstParam = mw.IsStatic ? 0 : 1;
		java.lang.Class[] parameters = new java.lang.Class[mw.GetParameters().Length + firstParam];
		for (int i = 0; i < mw.GetParameters().Length; i++)
		{
			parameters[i + firstParam] = mw.GetParameters()[i].ClassObject;
		}
		if (!mw.IsStatic)
		{
			parameters[0] = mw.DeclaringType.ClassObject;
		}
		self.vmtarget = CreateMemberNameDelegate(mw, mw.ReturnType.ClassObject, !wantSpecial, MethodType.methodType(mw.ReturnType.ClassObject, parameters));
#endif
	}

#if !FIRST_PASS
	private static void SetModifiers(MemberName self, MemberWrapper mw)
	{
		self._flags(self._flags() | (int)mw.Modifiers);
	}
#endif

	public static void expand(MemberName self)
	{
		throw new NotImplementedException();
	}

	public static MemberName resolve(MemberName self, java.lang.Class caller)
	{
#if !FIRST_PASS
		switch (self.getReferenceKind())
		{
			case MethodHandleNatives.Constants.REF_invokeStatic:
				if (self.getDeclaringClass() == CoreClasses.java.lang.invoke.MethodHandle.Wrapper.ClassObject)
				{
					switch (self.getName())
					{
						case "linkToVirtual":
						case "linkToStatic":
						case "linkToSpecial":
						case "linkToInterface":
							// this delegate is never used normally, only by the PrivateInvokeTest white-box JSR-292 tests
							self.vmtarget = MethodHandleUtil.DynamicMethodBuilder.CreateMethodHandleLinkTo(self);
							self._flags(self._flags() | java.lang.reflect.Modifier.STATIC | java.lang.reflect.Modifier.NATIVE | MethodHandleNatives.Constants.MN_IS_METHOD);
							return self;
					}
				}
				ResolveMethod(self, caller);
				break;
			case MethodHandleNatives.Constants.REF_invokeVirtual:
				if (self.getDeclaringClass() == CoreClasses.java.lang.invoke.MethodHandle.Wrapper.ClassObject)
				{
					switch (self.getName())
					{
						case "invoke":
						case "invokeExact":
						case "invokeBasic":
							self.vmtarget = MethodHandleUtil.DynamicMethodBuilder.CreateMethodHandleInvoke(self);
							self._flags(self._flags() | java.lang.reflect.Modifier.NATIVE | java.lang.reflect.Modifier.FINAL | MethodHandleNatives.Constants.MN_IS_METHOD);
							return self;
					}
				}
				ResolveMethod(self, caller);
				break;
			case MethodHandleNatives.Constants.REF_invokeInterface:
			case MethodHandleNatives.Constants.REF_invokeSpecial:
			case MethodHandleNatives.Constants.REF_newInvokeSpecial:
				ResolveMethod(self, caller);
				break;
			case MethodHandleNatives.Constants.REF_getField:
			case MethodHandleNatives.Constants.REF_putField:
			case MethodHandleNatives.Constants.REF_getStatic:
			case MethodHandleNatives.Constants.REF_putStatic:
				ResolveField(self);
				break;
			default:
				throw new InvalidOperationException();
		}
#endif
		return self;
	}

#if !FIRST_PASS
	private static void ResolveMethod(MemberName self, java.lang.Class caller)
    {
#if !WINRT
        bool invokeSpecial = self.getReferenceKind() == MethodHandleNatives.Constants.REF_invokeSpecial;
		bool newInvokeSpecial = self.getReferenceKind() == MethodHandleNatives.Constants.REF_newInvokeSpecial;
		bool searchBaseClasses = !newInvokeSpecial;
		MethodWrapper mw = TypeWrapper.FromClass(self.getDeclaringClass()).GetMethodWrapper(self.getName(), self.getSignature().Replace('/', '.'), searchBaseClasses);
		if (mw == null)
		{
			if (self.getReferenceKind() == MethodHandleNatives.Constants.REF_invokeInterface)
			{
				mw = CoreClasses.java.lang.Object.Wrapper.GetMethodWrapper(self.getName(), self.getSignature().Replace('/', '.'), false);
				if (mw != null && mw.IsConstructor)
				{
					throw new java.lang.IncompatibleClassChangeError("Found interface " + self.getDeclaringClass().getName() + ", but class was expected");
				}
			}
			if (mw == null)
			{
				string msg = String.Format(invokeSpecial ? "{0}: method {1}{2} not found" : "{0}.{1}{2}", self.getDeclaringClass().getName(), self.getName(), self.getSignature());
				throw new java.lang.NoSuchMethodError(msg);
			}
		}
		if (mw.IsStatic != IsReferenceKindStatic(self.getReferenceKind()))
		{
			string msg = String.Format(mw.IsStatic ? "Expecting non-static method {0}.{1}{2}" : "Expected static method {0}.{1}{2}", mw.DeclaringType.Name, self.getName(), self.getSignature());
			throw new java.lang.IncompatibleClassChangeError(msg);
		}
		if (mw.IsConstructor && mw.DeclaringType == CoreClasses.java.lang.String.Wrapper)
		{
			typeof(MemberName).GetField("type", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(self, self.getMethodType().changeReturnType(typeof(string)));
			self.vmtarget = CreateMemberNameDelegate(mw, caller, false, self.getMethodType());
		}
		else if (!mw.IsConstructor || invokeSpecial || newInvokeSpecial)
		{
			MethodType methodType = self.getMethodType();
			if (!mw.IsStatic)
			{
				methodType = methodType.insertParameterTypes(0, mw.DeclaringType.ClassObject);
				if (newInvokeSpecial)
				{
					methodType = methodType.changeReturnType(java.lang.Void.TYPE);
				}
			}
			self.vmtarget = CreateMemberNameDelegate(mw, caller, self.hasReceiverTypeDispatch(), methodType);
		}
		SetModifiers(self, mw);
		self._flags(self._flags() | (mw.IsConstructor ? MethodHandleNatives.Constants.MN_IS_CONSTRUCTOR : MethodHandleNatives.Constants.MN_IS_METHOD));
		if (self.getReferenceKind() == MethodHandleNatives.Constants.REF_invokeVirtual && (mw.IsPrivate || mw.IsFinal || mw.IsConstructor))
		{
			int flags = self._flags();
			flags -= MethodHandleNatives.Constants.REF_invokeVirtual << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags += MethodHandleNatives.Constants.REF_invokeSpecial << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			self._flags(flags);
		}
		if (mw.HasCallerID || DynamicTypeWrapper.RequiresDynamicReflectionCallerClass(mw.DeclaringType.Name, mw.Name, mw.Signature))
		{
			self._flags(self._flags() | MemberName.CALLER_SENSITIVE);
		}
		if (mw.IsConstructor && mw.DeclaringType == CoreClasses.java.lang.String.Wrapper)
		{
			int flags = self._flags();
			flags -= MethodHandleNatives.Constants.REF_invokeSpecial << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags += MethodHandleNatives.Constants.REF_invokeStatic << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags -= MethodHandleNatives.Constants.MN_IS_CONSTRUCTOR;
			flags += MethodHandleNatives.Constants.MN_IS_METHOD;
			flags += MethodHandleNatives.Constants.ACC_STATIC;
			self._flags(flags);
        }
#else
            throw new NotImplementedException();
#endif
    }

	private static void ResolveField(MemberName self)
	{        
        FieldWrapper fw = TypeWrapper.FromClass(self.getDeclaringClass()).GetFieldWrapper(self.getName(), self.getSignature().Replace('/', '.'));
		if (fw == null)
		{
			throw new java.lang.NoSuchFieldError(self.getName());
		}
		SetModifiers(self, fw);
		self._flags(self._flags() | MethodHandleNatives.Constants.MN_IS_FIELD);
		if (fw.IsStatic != IsReferenceKindStatic(self.getReferenceKind()))
		{
			int newReferenceKind;
			switch (self.getReferenceKind())
			{
				case MethodHandleNatives.Constants.REF_getField:
					newReferenceKind = MethodHandleNatives.Constants.REF_getStatic;
					break;
				case MethodHandleNatives.Constants.REF_putField:
					newReferenceKind = MethodHandleNatives.Constants.REF_putStatic;
					break;
				case MethodHandleNatives.Constants.REF_getStatic:
					newReferenceKind = MethodHandleNatives.Constants.REF_getField;
					break;
				case MethodHandleNatives.Constants.REF_putStatic:
					newReferenceKind = MethodHandleNatives.Constants.REF_putField;
					break;
				default:
					throw new InvalidOperationException();
			}
			int flags = self._flags();
			flags -= self.getReferenceKind() << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			flags += newReferenceKind << MethodHandleNatives.Constants.MN_REFERENCE_KIND_SHIFT;
			self._flags(flags);
		}
	}
	
	private static bool IsReferenceKindStatic(int referenceKind)
	{
		switch (referenceKind)
		{
			case MethodHandleNatives.Constants.REF_getField:
			case MethodHandleNatives.Constants.REF_putField:
			case MethodHandleNatives.Constants.REF_invokeVirtual:
			case MethodHandleNatives.Constants.REF_invokeSpecial:
			case MethodHandleNatives.Constants.REF_newInvokeSpecial:
			case MethodHandleNatives.Constants.REF_invokeInterface:
				return false;
			case MethodHandleNatives.Constants.REF_getStatic:
			case MethodHandleNatives.Constants.REF_putStatic:
			case MethodHandleNatives.Constants.REF_invokeStatic:
				return true;
		}
		throw new InvalidOperationException();
	}
#endif

	// TODO consider caching this delegate in MethodWrapper
	private static Delegate CreateMemberNameDelegate(MethodWrapper mw, java.lang.Class caller, bool doDispatch, MethodType type)
	{
#if FIRST_PASS
		return null;
#elif !WINRT
        if (mw.IsDynamicOnly)
		{
			return MethodHandleUtil.DynamicMethodBuilder.CreateDynamicOnly(mw, type);
		}
		// HACK this code is duplicated in compiler.cs
		if (mw.IsProtected && (mw.DeclaringType == CoreClasses.java.lang.Object.Wrapper || mw.DeclaringType == CoreClasses.java.lang.Throwable.Wrapper))
		{
			TypeWrapper thisType = TypeWrapper.FromClass(caller);
			TypeWrapper cli_System_Object = ClassLoaderWrapper.LoadClassCritical("cli.System.Object");
			TypeWrapper cli_System_Exception = ClassLoaderWrapper.LoadClassCritical("cli.System.Exception");
			// HACK we may need to redirect finalize or clone from java.lang.Object/Throwable
			// to a more specific base type.
			if (thisType.IsAssignableTo(cli_System_Object))
			{
				mw = cli_System_Object.GetMethodWrapper(mw.Name, mw.Signature, true);
			}
			else if (thisType.IsAssignableTo(cli_System_Exception))
			{
				mw = cli_System_Exception.GetMethodWrapper(mw.Name, mw.Signature, true);
			}
			else if (thisType.IsAssignableTo(CoreClasses.java.lang.Throwable.Wrapper))
			{
				mw = CoreClasses.java.lang.Throwable.Wrapper.GetMethodWrapper(mw.Name, mw.Signature, true);
			}
		}
		TypeWrapper tw = mw.DeclaringType;
		tw.Finish();
		mw.Link();
		mw.ResolveMethod();
		MethodInfo mi = mw.GetMethod() as MethodInfo;
		if (mi != null
			&& !mw.HasCallerID
			&& mw.IsStatic
			&& MethodHandleUtil.HasOnlyBasicTypes(mw.GetParameters(), mw.ReturnType)
			&& type.parameterCount() <= MethodHandleUtil.MaxArity)
		{
			return Delegate.CreateDelegate(MethodHandleUtil.CreateMemberWrapperDelegateType(mw.GetParameters(), mw.ReturnType), mi);
		}
		else
		{
			// slow path where we emit a DynamicMethod
			return MethodHandleUtil.DynamicMethodBuilder.CreateMemberName(mw, type, doDispatch);
		}
#else
            throw new NotImplementedException();
#endif
    }

    public static int getMembers(java.lang.Class defc, string matchName, string matchSig, int matchFlags, java.lang.Class caller, int skip, MemberName[] results)
	{
#if FIRST_PASS
		return 0;
#else
		if (matchName != null || matchSig != null || matchFlags != MethodHandleNatives.Constants.MN_IS_METHOD)
		{
			throw new NotImplementedException();
		}
		MethodWrapper[] methods = TypeWrapper.FromClass(defc).GetMethods();
		for (int i = skip, len = Math.Min(results.Length, methods.Length - skip); i < len; i++)
		{
			if (!methods[i].IsConstructor && methods[i].Name != StringConstants.CLINIT)
			{
				results[i - skip] = new MemberName((java.lang.reflect.Method)methods[i].ToMethodOrConstructor(true), false);
			}
		}
		return methods.Length - skip;
#endif
	}

	public static long objectFieldOffset(MemberName self)
	{
#if FIRST_PASS
		return 0;
#else
        java.lang.reflect.Field field = (java.lang.reflect.Field)TypeWrapper.FromClass(self.getDeclaringClass())
			.GetFieldWrapper(self.getName(), self.getSignature().Replace('/', '.')).ToField(false);
		return sun.misc.Unsafe.allocateUnsafeFieldId(field);
#endif
	}

	public static long staticFieldOffset(MemberName self)
	{
		return objectFieldOffset(self);
	}

	public static object staticFieldBase(MemberName self)
	{
		return null;
	}

#if !FIRST_PASS
	internal static void InitializeCallSite(CallSite site)
	{
#if WINRT
        throw new NotImplementedException("InitializeCallSite");
#else
        Type type = typeof(IKVM.Runtime.IndyCallSite<>).MakeGenericType(MethodHandleUtil.GetDelegateTypeForInvokeExact(site.type()));
        IKVM.Runtime.IIndyCallSite ics = (IKVM.Runtime.IIndyCallSite)Activator.CreateInstance(type, true);
        System.Threading.Interlocked.CompareExchange(ref site.ics, ics, null);
#endif
    }
#endif

    public static void setCallSiteTargetNormal(CallSite site, MethodHandle target)
	{
#if !FIRST_PASS
		if (site.ics == null)
		{
			InitializeCallSite(site);
		}
		lock (site.ics)
		{
			site.target = target;
			site.ics.SetTarget(target);
		}
#endif
	}

	public static void setCallSiteTargetVolatile(CallSite site, MethodHandle target)
	{
		setCallSiteTargetNormal(site, target);
	}

	public static void registerNatives()
	{
	}

	public static object getMemberVMInfo(MemberName self)
	{
#if FIRST_PASS
		return null;
#else
		if (self.isField())
		{
			return new object[] { java.lang.Long.valueOf(0), self.getDeclaringClass() };
		}
		if (MethodHandleNatives.refKindDoesDispatch(self.getReferenceKind()))
		{
			return new object[] { java.lang.Long.valueOf(0), self };
		}
		return new object[] { java.lang.Long.valueOf(-1), self };
#endif
	}

	public static int getConstant(int which)
	{
		return 0;
	}

	public static int getNamedCon(int which, object[] name)
	{
#if FIRST_PASS
		return 0;
#else
#if !WINRT
        FieldInfo[] fields = typeof(MethodHandleNatives.Constants).GetFields(BindingFlags.Static | BindingFlags.NonPublic);
		if (which >= fields.Length)
		{
			name[0] = null;
			return -1;
		}
		name[0] = fields[which].Name;
		return ((IConvertible)fields[which].GetRawConstantValue()).ToInt32(null);
#else
            throw new NotImplementedException();
#endif
#endif
    }
}

static partial class MethodHandleUtil
{
	internal static Type GetMemberWrapperDelegateType(MethodType type)
	{
#if FIRST_PASS
		return null;
#else
		return GetDelegateTypeForInvokeExact(type.basicType());
#endif
	}

#if !FIRST_PASS
	private static Type CreateMethodHandleDelegateType(MethodType type)
	{
		TypeWrapper[] args = new TypeWrapper[type.parameterCount()];
		for (int i = 0; i < args.Length; i++)
		{
			args[i] = TypeWrapper.FromClass(type.parameterType(i));
			args[i].Finish();
		}
		TypeWrapper ret = TypeWrapper.FromClass(type.returnType());
		ret.Finish();
		return CreateMethodHandleDelegateType(args, ret);
	}

	private static Type[] GetParameterTypes(MethodBase mb)
	{
		ParameterInfo[] pi = mb.GetParameters();
		Type[] args = new Type[pi.Length];
		for (int i = 0; i < args.Length; i++)
		{
			args[i] = pi[i].ParameterType;
		}
		return args;
	}

	private static Type[] GetParameterTypes(Type thisType, MethodBase mb)
	{
		ParameterInfo[] pi = mb.GetParameters();
		Type[] args = new Type[pi.Length + 1];
		args[0] = thisType;
		for (int i = 1; i < args.Length; i++)
		{
			args[i] = pi[i - 1].ParameterType;
		}
		return args;
	}

	internal static MethodType GetDelegateMethodType(Type type)
	{
		java.lang.Class[] types;
		MethodInfo mi = GetDelegateInvokeMethod(type);
		ParameterInfo[] pi = mi.GetParameters();
		if (pi.Length > 0 && IsPackedArgsContainer(pi[pi.Length - 1].ParameterType))
		{
			System.Collections.Generic.List<java.lang.Class> list = new System.Collections.Generic.List<java.lang.Class>();
			for (int i = 0; i < pi.Length - 1; i++)
			{
				list.Add(ClassLoaderWrapper.GetWrapperFromType(pi[i].ParameterType).ClassObject);
			}
			Type[] args = pi[pi.Length - 1].ParameterType.GetGenericArguments();
			while (IsPackedArgsContainer(args[args.Length - 1]))
			{
				for (int i = 0; i < args.Length - 1; i++)
				{
					list.Add(ClassLoaderWrapper.GetWrapperFromType(args[i]).ClassObject);
				}
				args = args[args.Length - 1].GetGenericArguments();
			}
			for (int i = 0; i < args.Length; i++)
			{
				list.Add(ClassLoaderWrapper.GetWrapperFromType(args[i]).ClassObject);
			}
			types = list.ToArray();
		}
		else
		{
			types = new java.lang.Class[pi.Length];
			for (int i = 0; i < types.Length; i++)
			{
				types[i] = ClassLoaderWrapper.GetWrapperFromType(pi[i].ParameterType).ClassObject;
			}
		}
		return MethodType.methodType(ClassLoaderWrapper.GetWrapperFromType(mi.ReturnType).ClassObject, types);
	}

	internal sealed class DynamicMethodBuilder
	{
		private readonly MethodType type;
		private readonly int firstArg;
		private readonly Type delegateType;
		private readonly object firstBoundValue;
		private readonly object secondBoundValue;
		private readonly Type container;
#if !WINRT
        private readonly DynamicMethod dm;
#endif
        private readonly CodeEmitter ilgen;
		private readonly Type packedArgType;
		private readonly int packedArgPos;

		sealed class Container<T1, T2>
		{
			public T1 target;
			public T2 value;

			public Container(T1 target, T2 value)
			{
				this.target = target;
				this.value = value;
			}
		}

		private DynamicMethodBuilder(string name, MethodType type, Type container, object target, object value, Type owner, bool useBasicTypes)
        {
#if !WINRT

            this.type = type;
			this.delegateType = useBasicTypes ? GetMemberWrapperDelegateType(type) : GetDelegateTypeForInvokeExact(type);
			this.firstBoundValue = target;
			this.secondBoundValue = value;
			this.container = container;
			MethodInfo mi = GetDelegateInvokeMethod(delegateType);
			Type[] paramTypes;
			if (container != null)
			{
				this.firstArg = 1;
				paramTypes = GetParameterTypes(container, mi);
			}
			else if (target != null)
			{
				this.firstArg = 1;
				paramTypes = GetParameterTypes(target.GetType(), mi);
			}
			else
			{
				paramTypes = GetParameterTypes(mi);
			}
			if (!ReflectUtil.CanOwnDynamicMethod(owner))
			{
				owner = typeof(DynamicMethodBuilder);
			}
			this.dm = new DynamicMethod(name, mi.ReturnType, paramTypes, owner, true);
			this.ilgen = CodeEmitter.Create(dm);

			if (type.parameterCount() > MaxArity)
			{
				ParameterInfo[] pi = mi.GetParameters();
				this.packedArgType = pi[pi.Length - 1].ParameterType;
				this.packedArgPos = pi.Length - 1 + firstArg;
			}
			else
			{
				this.packedArgPos = Int32.MaxValue;
            }
#else
            throw new NotImplementedException();
#endif
        }

		internal static Delegate CreateVoidAdapter(MethodType type)
        {
#if !WINRT
            DynamicMethodBuilder dm = new DynamicMethodBuilder("VoidAdapter", type.changeReturnType(java.lang.Void.TYPE), null, null, null, null, true);
			Type targetDelegateType = GetMemberWrapperDelegateType(type);
			dm.Ldarg(0);
			dm.EmitCheckcast(CoreClasses.java.lang.invoke.MethodHandle.Wrapper);
			dm.ilgen.Emit(OpCodes.Ldfld, typeof(MethodHandle).GetField("form", BindingFlags.Instance | BindingFlags.NonPublic));
			dm.ilgen.Emit(OpCodes.Ldfld, typeof(LambdaForm).GetField("vmentry", BindingFlags.Instance | BindingFlags.NonPublic));
			dm.ilgen.Emit(OpCodes.Ldfld, typeof(MemberName).GetField("vmtarget", BindingFlags.Instance | BindingFlags.NonPublic));
			dm.ilgen.Emit(OpCodes.Castclass, targetDelegateType);
			for (int i = 0; i < type.parameterCount(); i++)
			{
				dm.Ldarg(i);
			}
			dm.CallDelegate(targetDelegateType);
			dm.ilgen.Emit(OpCodes.Pop);
			dm.Ret();
			return dm.CreateDelegate();
#else
            throw new NotImplementedException();
#endif
        }

		internal static DynamicMethod CreateInvokeExact(MethodType type)
        {
#if !WINRT
            FinishTypes(type);
			DynamicMethodBuilder dm = new DynamicMethodBuilder("InvokeExact", type, typeof(MethodHandle), null, null, null, false);
			Type targetDelegateType = GetMemberWrapperDelegateType(type.insertParameterTypes(0, CoreClasses.java.lang.invoke.MethodHandle.Wrapper.ClassObject));
			dm.ilgen.Emit(OpCodes.Ldarg_0);
			dm.ilgen.Emit(OpCodes.Ldfld, typeof(MethodHandle).GetField("form", BindingFlags.Instance | BindingFlags.NonPublic));
			dm.ilgen.Emit(OpCodes.Ldfld, typeof(LambdaForm).GetField("vmentry", BindingFlags.Instance | BindingFlags.NonPublic));
			if (type.returnType() == java.lang.Void.TYPE)
			{
				dm.ilgen.Emit(OpCodes.Call, typeof(MethodHandleUtil).GetMethod("GetVoidAdapter", BindingFlags.Static | BindingFlags.NonPublic));
			}
			else
			{
				dm.ilgen.Emit(OpCodes.Ldfld, typeof(MemberName).GetField("vmtarget", BindingFlags.Instance | BindingFlags.NonPublic));
			}
			dm.ilgen.Emit(OpCodes.Castclass, targetDelegateType);
			dm.ilgen.Emit(OpCodes.Ldarg_0);
			for (int i = 0; i < type.parameterCount(); i++)
			{
				dm.Ldarg(i);
				TypeWrapper tw = TypeWrapper.FromClass(type.parameterType(i));
				if (tw.IsNonPrimitiveValueType)
				{
					tw.EmitBox(dm.ilgen);
				}
				else if (tw.IsGhost)
				{
					tw.EmitConvSignatureTypeToStackType(dm.ilgen);
				}
				else if (tw == PrimitiveTypeWrapper.BYTE)
				{
					dm.ilgen.Emit(OpCodes.Conv_I1);
				}
			}
			dm.CallDelegate(targetDelegateType);
			TypeWrapper retType = TypeWrapper.FromClass(type.returnType());
			if (retType.IsNonPrimitiveValueType)
			{
				retType.EmitUnbox(dm.ilgen);
			}
			else if (retType.IsGhost)
			{
				retType.EmitConvStackTypeToSignatureType(dm.ilgen, null);
			}
			else if (!retType.IsPrimitive && retType != CoreClasses.java.lang.Object.Wrapper)
			{
				dm.EmitCheckcast(retType);
			}
			dm.Ret();
			dm.ilgen.DoEmit();
			return dm.dm;
#else
            throw new NotImplementedException();
#endif
        }

		internal static Delegate CreateMethodHandleLinkTo(MemberName mn)
        {
#if !WINRT
            MethodType type = mn.getMethodType();
			Type delegateType = MethodHandleUtil.GetMemberWrapperDelegateType(type.dropParameterTypes(type.parameterCount() - 1, type.parameterCount()));
			DynamicMethodBuilder dm = new DynamicMethodBuilder("DirectMethodHandle." + mn.getName() + type, type, null, null, null, null, true);
			dm.Ldarg(type.parameterCount() - 1);
			dm.ilgen.EmitCastclass(typeof(java.lang.invoke.MemberName));
			dm.ilgen.Emit(OpCodes.Ldfld, typeof(java.lang.invoke.MemberName).GetField("vmtarget", BindingFlags.Instance | BindingFlags.NonPublic));
			dm.ilgen.Emit(OpCodes.Castclass, delegateType);
			for (int i = 0, count = type.parameterCount() - 1; i < count; i++)
			{
				dm.Ldarg(i);
			}
			dm.CallDelegate(delegateType);
			dm.Ret();
			return dm.CreateDelegate();
#else
            throw new NotImplementedException();
#endif
        }

		internal static Delegate CreateMethodHandleInvoke(MemberName mn)
        {
#if !WINRT
            MethodType type = mn.getMethodType().insertParameterTypes(0, mn.getDeclaringClass());
			Type targetDelegateType = MethodHandleUtil.GetMemberWrapperDelegateType(type);
			DynamicMethodBuilder dm = new DynamicMethodBuilder("DirectMethodHandle." + mn.getName() + type, type,
				typeof(Container<,>).MakeGenericType(typeof(object), typeof(IKVM.Runtime.InvokeCache<>).MakeGenericType(targetDelegateType)), null, null, null, true);
			dm.Ldarg(0);
			dm.EmitCheckcast(CoreClasses.java.lang.invoke.MethodHandle.Wrapper);
			switch (mn.getName())
			{
				case "invokeExact":
					dm.Call(ByteCodeHelperMethods.GetDelegateForInvokeExact.MakeGenericMethod(targetDelegateType));
					break;
				case "invoke":
					dm.LoadValueAddress();
					dm.Call(ByteCodeHelperMethods.GetDelegateForInvoke.MakeGenericMethod(targetDelegateType));
					break;
				case "invokeBasic":
					dm.Call(ByteCodeHelperMethods.GetDelegateForInvokeBasic.MakeGenericMethod(targetDelegateType));
					break;
				default:
					throw new InvalidOperationException();
			}
			dm.Ldarg(0);
			for (int i = 1, count = type.parameterCount(); i < count; i++)
			{
				dm.Ldarg(i);
			}
			dm.CallDelegate(targetDelegateType);
			dm.Ret();
			return dm.CreateDelegate();
#else
            throw new NotImplementedException();
#endif
        }

        internal static Delegate CreateDynamicOnly(MethodWrapper mw, MethodType type)
        {
#if !WINRT
            FinishTypes(type);
			DynamicMethodBuilder dm = new DynamicMethodBuilder("CustomInvoke:" + mw.Name, type, null, mw, null, null, true);
			dm.ilgen.Emit(OpCodes.Ldarg_0);
			if (mw.IsStatic)
			{
				dm.LoadNull();
				dm.BoxArgs(0);
			}
			else
			{
				dm.Ldarg(0);
				dm.BoxArgs(1);
			}
			dm.Callvirt(typeof(MethodWrapper).GetMethod("Invoke", BindingFlags.Instance | BindingFlags.NonPublic));
			dm.UnboxReturnValue();
			dm.Ret();
			return dm.CreateDelegate();
#else
            throw new NotImplementedException();
#endif
        }

		internal sealed class DynamicCallerID : ikvm.@internal.CallerID
		{
			internal static readonly DynamicCallerID Instance = new DynamicCallerID();

			private DynamicCallerID() { }

			internal override java.lang.Class getAndCacheClass()
			{
#if false// !WINRT
                //  for (int i = 0, skip = 1; ; )
				//  {
				//  	MethodBase method = new StackFrame(i++, false).GetMethod();
				//  	if (method == null)
				//  	{
				//  		return null;
				//  	}
				//  	if (Java_sun_reflect_Reflection.IsHideFromStackWalk(method) || method.DeclaringType == typeof(ikvm.@internal.CallerID))
				//  	{
				//  		continue;
				//  	}
				//  	if (skip-- == 0)
				//  	{
				//  		return ClassLoaderWrapper.GetWrapperFromType(method.DeclaringType).ClassObject;
				//  	}
				//  }
#else
                throw new NotImplementedException();
#endif
            }

			internal override java.lang.ClassLoader getAndCacheClassLoader()
			{
				java.lang.Class clazz = getAndCacheClass();
				return clazz == null ? null : TypeWrapper.FromClass(clazz).GetClassLoader().GetJavaClassLoader();
			}
		}

		internal static Delegate CreateMemberName(MethodWrapper mw, MethodType type, bool doDispatch)
        {
#if !WINRT
            FinishTypes(type);
			TypeWrapper tw = mw.DeclaringType;
			Type owner = tw.TypeAsBaseType;
#if NET_4_0
			if (!doDispatch && !mw.IsStatic)
			{
				// on .NET 4 we can only do a non-virtual invocation of a virtual method if we skip verification,
				// and to skip verification we need to inject the dynamic method in a critical assembly

				// TODO instead of injecting in mscorlib, we should use DynamicMethodUtils.Create()
				owner = typeof(object);
			}
#endif
			DynamicMethodBuilder dm = new DynamicMethodBuilder("MemberName:" + mw.DeclaringType.Name + "::" + mw.Name + mw.Signature, type, null,
				mw.HasCallerID ? DynamicCallerID.Instance : null, null, owner, true);
			for (int i = 0, count = type.parameterCount(); i < count; i++)
			{
				if (i == 0 && !mw.IsStatic && (tw.IsGhost || tw.IsNonPrimitiveValueType || tw.IsRemapped) && (!mw.IsConstructor || tw != CoreClasses.java.lang.String.Wrapper))
				{
					if (tw.IsGhost || tw.IsNonPrimitiveValueType)
					{
						dm.LoadFirstArgAddress(tw);
					}
					else
					{
						Debug.Assert(tw.IsRemapped);
						// TODO this must be checked
						dm.Ldarg(0);
						if (mw.IsConstructor)
						{
							dm.EmitCastclass(tw.TypeAsBaseType);
						}
						else
						{
							dm.EmitCheckcast(tw);
						}
					}
				}
				else
				{
					dm.Ldarg(i);
					TypeWrapper argType = TypeWrapper.FromClass(type.parameterType(i));
					if (!argType.IsPrimitive)
					{
						if (argType.IsUnloadable)
						{
						}
						else if (argType.IsNonPrimitiveValueType)
						{
							dm.Unbox(argType);
						}
						else if (argType.IsGhost)
						{
							dm.UnboxGhost(argType);
						}
						else
						{
							dm.EmitCheckcast(argType);
						}
					}
				}
			}
			if (mw.HasCallerID)
			{
				dm.LoadCallerID();
			}
			if (doDispatch && !mw.IsStatic)
			{
				dm.Callvirt(mw);
			}
			else
			{
				dm.Call(mw);
			}
			TypeWrapper retType = TypeWrapper.FromClass(type.returnType());
			if (retType.IsUnloadable)
			{
			}
			else if (retType.IsNonPrimitiveValueType)
			{
				dm.Box(retType);
			}
			else if (retType.IsGhost)
			{
				dm.BoxGhost(retType);
			}
			else if (retType == PrimitiveTypeWrapper.BYTE)
			{
				dm.CastByte();
			}
			dm.Ret();
			return dm.CreateDelegate();
#else
            throw new NotImplementedException();
#endif
        }

		internal void Call(MethodInfo method)
        {
#if !WINRT
            ilgen.Emit(OpCodes.Call, method);
#else
            throw new NotImplementedException();
#endif
        }

		internal void Callvirt(MethodInfo method)
        {
#if !WINRT
            ilgen.Emit(OpCodes.Callvirt, method);
#else
            throw new NotImplementedException();
#endif
        }

		internal void Call(MethodWrapper mw)
        {
#if !WINRT
            mw.EmitCall(ilgen);
#else
            throw new NotImplementedException();
#endif
        }

		internal void Callvirt(MethodWrapper mw)
        {
#if !WINRT
            mw.EmitCallvirt(ilgen);
#else
            throw new NotImplementedException();
#endif
        }

		internal void CallDelegate(Type delegateType)
        {
#if !WINRT
            EmitCallDelegateInvokeMethod(ilgen, delegateType);
#else
            throw new NotImplementedException();
#endif
        }

		internal void LoadFirstArgAddress(TypeWrapper tw)
        {
#if !WINRT
            ilgen.EmitLdarg(0);
			if (tw.IsGhost)
			{
				tw.EmitConvStackTypeToSignatureType(ilgen, null);
				CodeEmitterLocal local = ilgen.DeclareLocal(tw.TypeAsSignatureType);
				ilgen.Emit(OpCodes.Stloc, local);
				ilgen.Emit(OpCodes.Ldloca, local);
			}
			else if (tw.IsNonPrimitiveValueType)
			{
				ilgen.Emit(OpCodes.Unbox, tw.TypeAsSignatureType);
			}
			else
			{
				throw new InvalidOperationException();
            }
#else
            throw new NotImplementedException();
#endif
        }

		internal void Ldarg(int i)
        {
#if !WINRT
            i += firstArg;
			if (i >= packedArgPos)
			{
				ilgen.EmitLdarga(packedArgPos);
				int fieldPos = i - packedArgPos;
				Type type = packedArgType;
				while (fieldPos >= MaxArity || (fieldPos == MaxArity - 1 && IsPackedArgsContainer(type.GetField("t8").FieldType)))
				{
					FieldInfo field = type.GetField("t8");
					type = field.FieldType;
					ilgen.Emit(OpCodes.Ldflda, field);
					fieldPos -= MaxArity - 1;
				}
				ilgen.Emit(OpCodes.Ldfld, type.GetField("t" + (1 + fieldPos)));
			}
			else
			{
				ilgen.EmitLdarg(i);
            }
#else
            throw new NotImplementedException();
#endif
        }

		internal void LoadCallerID()
        {
#if !WINRT
            ilgen.Emit(OpCodes.Ldarg_0);
#else
            throw new NotImplementedException();
#endif
        }

		internal void LoadValueAddress()
        {
#if !WINRT
            ilgen.Emit(OpCodes.Ldarg_0);
			ilgen.Emit(OpCodes.Ldflda, container.GetField("value"));
#else
            throw new NotImplementedException();
#endif
        }

		internal void Ret()
        {
#if !WINRT
            ilgen.Emit(OpCodes.Ret);
#else
            throw new NotImplementedException();
#endif
        }

		internal Delegate CreateDelegate()
		{
#if WINRT
            throw new NotImplementedException("CreateDelegate");
#else
           // Console.WriteLine(delegateType);
            ilgen.DumpMethod();
            ilgen.DoEmit();
            return ValidateDelegate(firstArg == 0
                ? dm.CreateDelegate(delegateType)
                : dm.CreateDelegate(delegateType, container == null ? firstBoundValue : Activator.CreateInstance(container, firstBoundValue, secondBoundValue)));
#endif
        }

		internal void BoxArgs(int start)
        {
#if !WINRT
            int paramCount = type.parameterCount();
			ilgen.EmitLdc_I4(paramCount - start);
			ilgen.Emit(OpCodes.Newarr, Types.Object);
			for (int i = start; i < paramCount; i++)
			{
				ilgen.Emit(OpCodes.Dup);
				ilgen.EmitLdc_I4(i - start);
				Ldarg(i);
				TypeWrapper tw = TypeWrapper.FromClass(type.parameterType(i));
				if (tw.IsPrimitive)
				{
					ilgen.Emit(OpCodes.Box, tw.TypeAsSignatureType);
				}
				ilgen.Emit(OpCodes.Stelem_Ref);
            }
#else
            throw new NotImplementedException();
#endif
        }

		internal void UnboxReturnValue()
        {
#if !WINRT
            TypeWrapper tw = TypeWrapper.FromClass(type.returnType());
			if (tw == PrimitiveTypeWrapper.VOID)
			{
				ilgen.Emit(OpCodes.Pop);
			}
			else if (tw.IsPrimitive)
			{
				ilgen.Emit(OpCodes.Unbox, tw.TypeAsSignatureType);
				ilgen.Emit(OpCodes.Ldobj, tw.TypeAsSignatureType);
            }
#else
            throw new NotImplementedException();
#endif
        }

		internal void LoadNull()
        {
#if !WINRT
            ilgen.Emit(OpCodes.Ldnull);
#else
            throw new NotImplementedException();
#endif
        }

		internal void Unbox(TypeWrapper tw)
        {
#if !WINRT
            tw.EmitUnbox(ilgen);
#else
            throw new NotImplementedException();
#endif
        }

		internal void Box(TypeWrapper tw)
        {
#if !WINRT
            tw.EmitBox(ilgen);
#else
            throw new NotImplementedException();
#endif
        }

		internal void UnboxGhost(TypeWrapper tw)
        {
#if !WINRT
            tw.EmitConvStackTypeToSignatureType(ilgen, null);
#else
            throw new NotImplementedException();
#endif
        }

		internal void BoxGhost(TypeWrapper tw)
        {
#if !WINRT
            tw.EmitConvSignatureTypeToStackType(ilgen);
#else
            throw new NotImplementedException();
#endif
        }

		internal void EmitCheckcast(TypeWrapper tw)
        {
#if !WINRT
            tw.EmitCheckcast(ilgen);
#else
            throw new NotImplementedException();
#endif
        }

		internal void EmitCastclass(Type type)
        {
#if !WINRT
            ilgen.EmitCastclass(type);
#else
            throw new NotImplementedException();
#endif
        }

		internal void EmitWriteLine()
        {
#if !WINRT
            ilgen.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(object) }));
#else
            throw new NotImplementedException();
#endif
        }

		internal void CastByte()
        {
#if !WINRT
            ilgen.Emit(OpCodes.Conv_I1);
#else
            throw new NotImplementedException();
#endif
        }

		internal void DumpMethod()
        {
#if !WINRT
            //	Console.WriteLine(dm.Name + ", type = " + delegateType);
            ilgen.DumpMethod();
#else
            throw new NotImplementedException();
#endif
        }

		private static void FinishTypes(MethodType type)
        {
#if !WINRT
            // FXBUG(?) DynamicILGenerator doesn't like SymbolType (e.g. an array of a TypeBuilder)
            // so we have to finish the signature types
            TypeWrapper.FromClass(type.returnType()).Finish();
			for (int i = 0; i < type.parameterCount(); i++)
			{
				TypeWrapper.FromClass(type.parameterType(i)).Finish();
            }
#else
            throw new NotImplementedException();
#endif
        }
	}

#if DEBUG
	[System.Security.SecuritySafeCritical]
#endif
	private static Delegate ValidateDelegate(Delegate d)
	{
#if DEBUG
		try
		{
			System.Runtime.CompilerServices.RuntimeHelpers.PrepareDelegate(d);
		}
		catch (Exception x)
		{
			JVM.CriticalFailure("Delegate failed to JIT", x);
		}
#endif
		return d;
	}

	internal static Type GetDelegateTypeForInvokeExact(MethodType type)
	{
		if (type._invokeExactDelegateType == null)
		{
			type._invokeExactDelegateType = CreateMethodHandleDelegateType(type);
		}
		return type._invokeExactDelegateType;
	}

	internal static T GetDelegateForInvokeExact<T>(MethodHandle mh)
		where T : class
    {
#if false//!WINRT
        MethodType type = mh.type();
		if (mh._invokeExactDelegate == null)
		{
			if (type._invokeExactDynamicMethod == null)
			{
				type._invokeExactDynamicMethod = DynamicMethodBuilder.CreateInvokeExact(type);
			}
			mh._invokeExactDelegate = type._invokeExactDynamicMethod.CreateDelegate(GetDelegateTypeForInvokeExact(type), mh);
			T del = mh._invokeExactDelegate as T;
			if (del != null)
			{
				return del;
			}
		}
		throw Invokers.newWrongMethodTypeException(GetDelegateMethodType(typeof(T)), type);
#else
            throw new NotImplementedException();
#endif
    }

	// called from InvokeExact DynamicMethod and ByteCodeHelper.GetDelegateForInvokeBasic()
	internal static object GetVoidAdapter(MemberName mn)
	{
		MethodType type = mn.getMethodType();
		if (type.voidAdapter == null)
		{
			if (type.returnType() == java.lang.Void.TYPE)
			{
				return mn.vmtarget;
			}
			type.voidAdapter = DynamicMethodBuilder.CreateVoidAdapter(type);
		}
		return type.voidAdapter;
	}
#endif
        }

using AspectInjector.Broker;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Aop.Aspects
{
    [Aspect(Scope.Global)]
    [Injection(typeof(DummyAspect))]
    public class DummyAspect : Attribute
    {
        private static MethodInfo _asyncHandler = typeof(DummyAspect).GetMethod(nameof(WrapAsync), BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo _asyncGenericHandler = typeof(DummyAspect).GetMethod(nameof(WrapAsyncGeneric), BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo _syncHandler = typeof(DummyAspect).GetMethod(nameof(WrapSync), BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo _syncGenericHandler = typeof(DummyAspect).GetMethod(nameof(WrapSyncGeneric), BindingFlags.NonPublic | BindingFlags.Static);

        [Advice(Kind.Around, Targets = Target.Method)]
        public object LogCall(
            [Argument(Source.Target)] Func<object[], object> target,
            [Argument(Source.Arguments)] object[] args,
            [Argument(Source.ReturnType)] Type retType,
            [Argument(Source.Name)] string name
        )
        {
            try
            {
                if (typeof(Task).IsAssignableFrom(retType))
                {
                    if (retType == typeof(Task))
                        return _asyncHandler.Invoke(this, new object[] { target, args, name });
                    else
                    {
                        var syncResultType = retType.IsConstructedGenericType ? retType.GenericTypeArguments[0] : typeof(object);
                        return _asyncGenericHandler.MakeGenericMethod(syncResultType).Invoke(this, new object[] { target, args, name });
                    }
                }
                else
                {
                    if (retType == typeof(void))
                        return _syncHandler.Invoke(this, new object[] { target, args, name });
                    else
                        return _syncGenericHandler.MakeGenericMethod(retType).Invoke(this, new object[] { target, args, name });
                }
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                return default;
            }
        }

        private static void RunBeforeLogic() { }
        private static void RunAfterLogic() { }

        private static void WrapSync(Func<object[], object> target, object[] args, string name)
        {
            RunBeforeLogic();
            target(args);
            RunAfterLogic();
        }

        private static T WrapSyncGeneric<T>(Func<object[], object> target, object[] args, string name)
        {
            RunBeforeLogic();
            var result = (T)target(args);
            RunAfterLogic();
            return result;
        }

        private static async Task WrapAsync(Func<object[], object> target, object[] args, string name)
        {
            RunBeforeLogic();
            await (Task)target(args);
            RunAfterLogic();
        }

        private static async Task<T> WrapAsyncGeneric<T>(Func<object[], object> target, object[] args, string name)
        {
            RunBeforeLogic();
            var result = await (Task<T>)target(args);
            RunAfterLogic();
            return result;
        }
    }
}

using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MinorAlchemy
{
    public class MaTween<T>
    {
        public float elapsed = 0.0f, duration = 0.0f;
        public EaseType easeType = EaseType.Linear;
        public bool doCompleteOnStop = false;
        public bool isPaused;
        public bool isPlaying;
        public float Delay { get; set; }
        public T from = default(T);
        public T to = default(T);
        public T current = default(T);

        public Action<T> OnPlay = (T val) => { };
        public Action<T> OnUpdate = (T val) => { };
        public Action<T> OnComplete = (T val) => { };
        public Action<T> OnPause = (T val) => { };
        public Action<T> OnResume = (T val) => { };
        public Action<T> OnStop = (T val) => { };

        public event Action<T> PlayEvent = (T val) => { };
        public event Action<T> UpdateEvent = (T val) => { };
        public event Action<T> CompleteEvent = (T val) => { };
        public event Action<T> PauseEvent = (T val) => { };
        public event Action<T> ResumeEvent = (T val) => { };
        public event Action<T> StopEvent = (T val) => { };

        internal Action<float> UpdateTime = (float time) => { };

        TweenEngine tweenEngine;
        Easer easer = Ease.FromType(EaseType.Linear);

        public MaTween(T from, T to, float duration, EaseType easeType)
        {
            this.from = from;
            this.to = to;
            this.duration = duration;
            this.easeType = easeType;
            easer = Ease.FromType(easeType);
            ITweenVal<T> tweenVal = TweenDelegate(typeof(T).Name);
            UpdateTime = (float time) => {
                current = (T)(object)(tweenVal.Val(easer(elapsed / duration)));
                OnUpdate(current);
            };
        }

        public MaTween(T from, T to, float duration, EaseType easeType, Action<T> OnUpdate) : this(from, to, duration, easeType)
        {
            this.OnUpdate = OnUpdate;
        }

        public MaTween(T from, T to, float duration, EaseType easeType, Action<T> OnUpdate, Action<T> OnComplete) : this(from, to, duration, easeType, OnUpdate)
        {
            this.OnComplete = OnComplete;
        }

        public void Play()
        {
            isPaused = false;
            elapsed = 0;
            easer = Ease.FromType(easeType);
            if(tweenEngine!=null)
                tweenEngine.AtomicStop();
            tweenEngine = TweenEngine.Instance;
            tweenEngine.Run(this);
            OnPlay(current);
            PlayEvent(current);
        }

        public void Resume()
        {
            isPaused = false;
            OnResume(current);
            ResumeEvent(current);
        }

        public void Pause()
        {
            isPaused = true;
            OnPause(current);
            PauseEvent(current);
        }

        public void Stop()
        {
            if (tweenEngine != null)
                tweenEngine.Stop();
            OnStop(current);
            StopEvent(current);
        }

        ITweenVal<T> TweenDelegate(string type)
        {
            switch (typeof(T).Name)
            {
                case "Single":
                    return (ITweenVal<T>)(object)new FloatVal(this as MaTween<float>);
                case "Vector2":
                    return (ITweenVal<T>)(object)new Vector2Val(this as MaTween<Vector2>);
                case "Vector3":
                    return (ITweenVal<T>)(object)new Vector3Val(this as MaTween<Vector3>);
                case "Vector4":
                    return (ITweenVal<T>)(object)new Vector4Val(this as MaTween<Vector4>);
                case "Quaternion":
                    return (ITweenVal<T>)(object)new QuatVal(this as MaTween<Quaternion>);
                case "Color":
                    return (ITweenVal<T>)(object)new ColorVal(this as MaTween<Color>);
                case "Rect":
                    return (ITweenVal<T>)(object)new RectVal(this as MaTween<Rect>);
            }
            return null;
        }
    }

    internal class TweenEngine : MonoBehaviour
    {
        static GameObject tweenContainer = null;

        public static GameObject TweenContainer
        {
            get  { return (tweenContainer = tweenContainer == null ? new GameObject("Tweens") : tweenContainer); }
        }
        static HashSet<TweenEngine> Instances = new HashSet<TweenEngine>();
        bool isAvailable = true;

        public static TweenEngine Instance
        {
            get
            {
                TweenEngine instance = Instances.FirstOrDefault(x => x.isAvailable) == null ? new GameObject("Tween Engine").AddComponent<TweenEngine>() : Instances.FirstOrDefault(x => x.isAvailable);
                instance.transform.parent = TweenContainer.transform;
                Instances.Add(instance);
                return instance;
            }
        }

        void OnDestroy()
        {
            Instances.ToList().ForEach(x => Destroy(x.gameObject));
            Instances.Clear();
        }

        public virtual void AtomicStop()
        {
            Stop();
            StopAllCoroutines();
        }

        public virtual void Stop()
        {
            isAvailable = true;
        }

        public virtual void Run<T>(MaTween<T> tween)
        {
            isAvailable = false;
            StartCoroutine(Loop(tween));
        }

        public IEnumerator Loop<T>(MaTween<T> tween)
        {
            yield return new WaitForSeconds(tween.Delay);
            while (tween.elapsed < tween.duration && !isAvailable)
            {
                tween.UpdateTime(tween.elapsed);
                tween.elapsed = Mathf.MoveTowards(tween.elapsed, tween.duration, Time.deltaTime);
                yield return new WaitForEndOfFrame();
                if (tween.isPaused)
                    yield return StartCoroutine(PauseRoutine<T>(tween));
            }
            isAvailable = true;
            if (tween.elapsed >= tween.duration || tween.doCompleteOnStop)
                tween.OnComplete(tween.to);
        }

        protected virtual IEnumerator PauseRoutine<T>(MaTween<T> tween)
        {
            while (!isAvailable && tween.isPaused)
                yield return null;
        }
    }

    public delegate float Easer(float t);

    public enum EaseType
    {
        Linear = 0x000001,
        QuadIn = 0x000002,
        QuadOut = 0x000004,
        QuadInOut = 0x000008,
        CubeIn = 0x000016,
        CubeOut = 0x000032,
        CubeInOut = 0x000064,
        BackIn = 0x000128,
        BackOut = 0x000256,
        BackInOut = 0x000512,
        ExpoIn = 0x001024,
        ExpoOut = 0x002048,
        ExpoInOut = 0x004096,
        SineIn = 0x008192,
        SineOut = 0x016384,
        SineInOut = 0x032768,
        ElasticIn = 0x065536,
        ElasticOut = 0x131072,
        ElasticInOut = 0x262144,
    }

    public static class Ease
    {
        public static readonly Easer Linear = (t) => {
            return t;
        };
        public static readonly Easer QuadIn = (t) => {
            return t * t;
        };
        public static readonly Easer QuadOut = (t) => {
            return 1 - QuadIn(1 - t);
        };
        public static readonly Easer QuadInOut = (t) => {
            return (t <= 0.5f) ? QuadIn(t * 2) / 2 : QuadOut(t * 2 - 1) / 2 + 0.5f;
        };
        public static readonly Easer CubeIn = (t) => {
            return t * t * t;
        };
        public static readonly Easer CubeOut = (t) => {
            return 1 - CubeIn(1 - t);
        };
        public static readonly Easer CubeInOut = (t) => {
            return (t <= 0.5f) ? CubeIn(t * 2) / 2 : CubeOut(t * 2 - 1) / 2 + 0.5f;
        };
        public static readonly Easer BackIn = (t) => {
            return t * t * (2.70158f * t - 1.70158f);
        };
        public static readonly Easer BackOut = (t) => {
            return 1 - BackIn(1 - t);
        };
        public static readonly Easer BackInOut = (t) => {
            return (t <= 0.5f) ? BackIn(t * 2) / 2 : BackOut(t * 2 - 1) / 2 + 0.5f;
        };
        public static readonly Easer ExpoIn = (t) => {
            return (float)Mathf.Pow(2, 10 * (t - 1));
        };
        public static readonly Easer ExpoOut = (t) => {
            return 1 - ExpoIn(t);
        };
        public static readonly Easer ExpoInOut = (t) => {
            return t < .5f ? ExpoIn(t * 2) / 2 : ExpoOut(t * 2) / 2;
        };
        public static readonly Easer SineIn = (t) => {
            return -Mathf.Cos(Mathf.PI / 2 * t) + 1;
        };
        public static readonly Easer SineOut = (t) => {
            return Mathf.Sin(Mathf.PI / 2 * t);
        };
        public static readonly Easer SineInOut = (t) => {
            return -Mathf.Cos(Mathf.PI * t) / 2f + .5f;
        };
        public static readonly Easer ElasticIn = (t) => {
            return 1 - ElasticOut(1 - t);
        };
        public static readonly Easer ElasticOut = (t) => {
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - 0.075f) * (2 * Mathf.PI) / 0.3f) + 1;
        };
        public static readonly Easer ElasticInOut = (t) => {
            return (t <= 0.5f) ? ElasticIn(t * 2) / 2 : ElasticOut(t * 2 - 1) / 2 + 0.5f;
        };

        public static Easer FromType(EaseType type)
        {
            switch (type)
            {
                case EaseType.Linear:
                    return Linear;
                case EaseType.QuadIn:
                    return QuadIn;
                case EaseType.QuadOut:
                    return QuadOut;
                case EaseType.QuadInOut:
                    return QuadInOut;
                case EaseType.CubeIn:
                    return CubeIn;
                case EaseType.CubeOut:
                    return CubeOut;
                case EaseType.CubeInOut:
                    return CubeInOut;
                case EaseType.BackIn:
                    return BackIn;
                case EaseType.BackOut:
                    return BackOut;
                case EaseType.BackInOut:
                    return BackInOut;
                case EaseType.ExpoIn:
                    return ExpoIn;
                case EaseType.ExpoOut:
                    return ExpoOut;
                case EaseType.ExpoInOut:
                    return ExpoInOut;
                case EaseType.SineIn:
                    return SineIn;
                case EaseType.SineOut:
                    return SineOut;
                case EaseType.SineInOut:
                    return SineInOut;
                case EaseType.ElasticIn:
                    return ElasticIn;
                case EaseType.ElasticOut:
                    return ElasticOut;
                case EaseType.ElasticInOut:
                    return ElasticInOut;
            }
            return Linear;
        }
    }

    interface ITweenVal<T>
    {
        T Val(float time);
    }

    class TweenVal<T>
    {
        protected MaTween<T> tween;
        public TweenVal(MaTween<T> tween)
        {
            this.tween = tween;
        }
    }

    class FloatVal : TweenVal<float>, ITweenVal<float>
    {
        public FloatVal(MaTween<float> tween) : base(tween) { }
        public float Val(float time) { return Mathf.Lerp(tween.from, tween.to, time); }
    }

    class Vector2Val : TweenVal<Vector2>, ITweenVal<Vector2>
    {
        public Vector2Val(MaTween<Vector2> tween) : base(tween) { }
        public Vector2 Val(float time) { return Vector2.Lerp(tween.from, tween.to, time); }
    }

    class Vector3Val : TweenVal<Vector3>, ITweenVal<Vector3>
    {
        public Vector3Val(MaTween<Vector3> tween) : base(tween) { }
        public Vector3 Val(float time) { return Vector3.Lerp(tween.from, tween.to, time); }
    }

    class Vector4Val : TweenVal<Vector4>, ITweenVal<Vector4>
    {
        public Vector4Val(MaTween<Vector4> tween) : base(tween) { }
        public Vector4 Val(float time) { return Vector4.Lerp(tween.from, tween.to, time); }
    }

    class ColorVal : TweenVal<Color>, ITweenVal<Color>
    {
        public ColorVal(MaTween<Color> tween) : base(tween) { }
        public Color Val(float time) { return Color.Lerp(tween.from, tween.to, time); }
    }

    class RectVal : TweenVal<Rect>, ITweenVal<Rect>
    {
        public RectVal(MaTween<Rect> tween) : base(tween) { }
        public Rect Val(float time)
        {
            return new Rect(
                Mathf.Lerp(tween.from.x, tween.to.y, time),
                Mathf.Lerp(tween.from.y, tween.to.y, time),
                Mathf.Lerp(tween.from.width, tween.to.width, time),
                Mathf.Lerp(tween.from.height, tween.to.height, time));
        }
    }

    class QuatVal : TweenVal<Quaternion>, ITweenVal<Quaternion>
    {
        public QuatVal(MaTween<Quaternion> tween) : base(tween) { }
        public Quaternion Val(float time) { return Quaternion.Slerp(tween.from, tween.to, time); }
    }
}
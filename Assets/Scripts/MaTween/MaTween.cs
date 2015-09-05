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
		public bool willCompleteOnStop = false;
		public bool isPaused;
		public bool isPlaying;
		public float Delay { get; set; }
        public T from = default(T);
        public T to = default(T);
		internal Coroutine coroutine;

		TweenEngine tweenEngine;
		Easer easer = Ease.FromType(EaseType.Linear);
        
        public Action<T> Update;
        public Action<T> Complete;

        internal Action<float> UpdateTime = (float time) => { };

		public MaTween(T from, T to, float duration, EaseType easeType)
		{
			this.from = from;
			this.to = to;
			this.duration = duration;
            this.easeType = easeType;
			this.easer = Ease.FromType(easeType);
            
			tweenEngine = TweenEngine.Instance;
		}

        public MaTween(T from, T to, float duration, EaseType easeType, Action<T> Update) : this(from, to, duration, easeType) { 
            this.Update = Update;
        }

        public MaTween(T from, T to, float duration, EaseType easeType , Action<T> Update, Action<T> Complete) : this(from, to, duration, easeType, Update) {
            this.Complete = Complete; 
        }
		
		public void Play ()
		{
            ITweenVal<T> tweenVal = TweenDelegate(typeof(T).Name);
            this.UpdateTime = (float time) => { Update((T)(object)(tweenVal.Val(from, to, easer(elapsed / duration)))); }; 
			elapsed = 0;
			easer = Ease.FromType(easeType);
			Stop ();
			coroutine = tweenEngine.Run<T> (this);
		}
		
		public void Stop()
		{
			if (tweenEngine != null && coroutine != null)
				tweenEngine.Stop<T> (coroutine);
		}

        ITweenVal<T> TweenDelegate(string type)
        {
            switch (typeof(T).Name)
			{
			    case "Single" :
                    return (ITweenVal<T>)(object)new FloatVal((float)(object)from, (float)(object)to);
                case "Vector2" :
                    return (ITweenVal<T>)(object)new Vector2Val((Vector2)(object)from, (Vector2)(object)to);
                 case "Vector3" :
                    return (ITweenVal<T>)(object)new Vector3Val((Vector3)(object)from, (Vector3)(object)to);
                case "Vector4" :
                    return (ITweenVal<T>)(object)new Vector4Val((Vector4)(object)from, (Vector4)(object)to);
                case "Quaternion" :
                    return (ITweenVal<T>)(object)new QuatVal((Quaternion)(object)from, (Quaternion)(object)to);
                case "Color" :
                    return (ITweenVal<T>)(object)new ColorVal((Color)(object)from, (Color)(object)to);
                case "Rect" :
                    return (ITweenVal<T>)(object)new RectVal((Rect)(object)from, (Rect)(object)to);
            }
            return null;
        }
	}
	
	internal class TweenEngine : MonoBehaviour
	{
		static TweenEngine instance = null;
		
		public static TweenEngine Instance
		{
			get{
				instance = instance == null? new GameObject("Tween Engine").AddComponent<TweenEngine>() : instance;
				return instance;
			}
		}
		
		public virtual void Stop<T>(Coroutine routine)
		{
			if(routine!=null) StopCoroutine (routine);
		}
		
		public virtual Coroutine Run<T>(MaTween<T> tween)
		{
			return StartCoroutine(Loop<T> (tween));
		}
		
		public IEnumerator Loop<T> (MaTween<T> tween)
		{
			yield return new WaitForSeconds(tween.Delay);
			while(tween.elapsed < tween.duration)
			{
                tween.UpdateTime(tween.elapsed);
				tween.elapsed = Mathf.MoveTowards(tween.elapsed, tween.duration, Time.deltaTime);
				yield return new WaitForEndOfFrame();
				if(tween.isPaused)
					yield return StartCoroutine(PauseRoutine(tween.coroutine));
				
			}
            if (tween.coroutine != null || tween.willCompleteOnStop)
                tween.Complete(tween.to);
			
		}
		
		protected virtual IEnumerator PauseRoutine(Coroutine routine)
		{
			while(routine != null)
				yield return null;
		}
	}
	
	public delegate float Easer(float t);
	
	public enum EaseType
	{
		Linear 			= 0x000001,
		QuadIn 			= 0x000002,
		QuadOut 		= 0x000004,
		QuadInOut 		= 0x000008,
		CubeIn 			= 0x000016,
		CubeOut 		= 0x000032,
		CubeInOut 		= 0x000064,
		BackIn 			= 0x000128,
		BackOut 		= 0x000256,
		BackInOut 		= 0x000512,
		ExpoIn 			= 0x001024,
		ExpoOut 		= 0x002048,
		ExpoInOut 		= 0x004096,
		SineIn 			= 0x008192,
		SineOut 		= 0x016384,
		SineInOut 		= 0x032768,
		ElasticIn 		= 0x065536,
		ElasticOut 		= 0x131072,
		ElasticInOut 	= 0x262144,
	}
	
	public static class Ease
	{
		public static readonly Easer Linear = (t) => {
			return t; };
		public static readonly Easer QuadIn = (t) => {
			return t * t; };
		public static readonly Easer QuadOut = (t) => {
			return 1 - QuadIn (1 - t); };
		public static readonly Easer QuadInOut = (t) => {
			return (t <= 0.5f) ? QuadIn (t * 2) / 2 : QuadOut (t * 2 - 1) / 2 + 0.5f; };
		public static readonly Easer CubeIn = (t) => {
			return t * t * t; };
		public static readonly Easer CubeOut = (t) => {
			return 1 - CubeIn (1 - t); };
		public static readonly Easer CubeInOut = (t) => {
			return (t <= 0.5f) ? CubeIn (t * 2) / 2 : CubeOut (t * 2 - 1) / 2 + 0.5f; };
		public static readonly Easer BackIn = (t) => {
			return t * t * (2.70158f * t - 1.70158f); };
		public static readonly Easer BackOut = (t) => {
			return 1 - BackIn (1 - t); };
		public static readonly Easer BackInOut = (t) => {
			return (t <= 0.5f) ? BackIn (t * 2) / 2 : BackOut (t * 2 - 1) / 2 + 0.5f; };
		public static readonly Easer ExpoIn = (t) => {
			return (float)Mathf.Pow (2, 10 * (t - 1)); };
		public static readonly Easer ExpoOut = (t) => {
			return 1 - ExpoIn (t); };
		public static readonly Easer ExpoInOut = (t) => {
			return t < .5f ? ExpoIn (t * 2) / 2 : ExpoOut (t * 2) / 2; };
		public static readonly Easer SineIn = (t) => {
			return -Mathf.Cos (Mathf.PI / 2 * t) + 1; };
		public static readonly Easer SineOut = (t) => {
			return Mathf.Sin (Mathf.PI / 2 * t); };
		public static readonly Easer SineInOut = (t) => {
			return -Mathf.Cos (Mathf.PI * t) / 2f + .5f; };
		public static readonly Easer ElasticIn = (t) => {
			return 1 - ElasticOut (1 - t); };
		public static readonly Easer ElasticOut = (t) => {
			return Mathf.Pow (2, -10 * t) * Mathf.Sin ((t - 0.075f) * (2 * Mathf.PI) / 0.3f) + 1; };
		public static readonly Easer ElasticInOut = (t) => {
			return (t <= 0.5f) ? ElasticIn (t * 2) / 2 : ElasticOut (t * 2 - 1) / 2 + 0.5f; };
		
		public static Easer FromType (EaseType type)
		{
			switch (type) {
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
        T Val(T from, T to, float time);
    }

    class TweenVal<T>
    {
        protected T to = default(T);
        protected T from = default(T);
        public TweenVal(T from, T to)
        {
            this.from = from;
            this.to = to;
        } 
    }

    class FloatVal : TweenVal<float>, ITweenVal<float>
    {
        public FloatVal(float from, float to) : base(from, to) { }
        public float Val(float from, float To, float time) { return Mathf.Lerp(from, to, time); }
    }

    class Vector2Val : TweenVal<Vector2>, ITweenVal<Vector2>
    {
        public Vector2Val(Vector2 from, Vector2 to) : base(from, to) { }
        public Vector2 Val(Vector2 from, Vector2 To, float time) { return Vector2.Lerp(from, to, time); }
    }

    class Vector3Val : TweenVal<Vector3>, ITweenVal<Vector3>
    {
        public Vector3Val(Vector3 from, Vector3 to) : base(from, to) { }
        public Vector3 Val(Vector3 from, Vector3 To, float time) { return Vector3.Lerp(from, to, time); }
    }

    class Vector4Val : TweenVal<Vector4>, ITweenVal<Vector4>
    {
        public Vector4Val(Vector4 from, Vector4 to) : base(from, to) { }
        public Vector4 Val(Vector4 from, Vector4 To, float time) { return Vector4.Lerp(from, to, time); }
    }

    class ColorVal : TweenVal<Color>, ITweenVal<Color>
    {
        public ColorVal(Color from, Color to) : base(from, to) { }
        public Color Val(Color from, Color To, float time) { return Color.Lerp(from, to, time); }
    }

    class RectVal : TweenVal<Rect>, ITweenVal<Rect>
    {
        public RectVal(Rect from, Rect to) : base(from, to) { }
        public Rect Val(Rect from, Rect to, float time)
        {
            return new Rect(
                Mathf.Lerp(from.x, to.y, time),
                Mathf.Lerp(from.y, to.y, time),
                Mathf.Lerp(from.width, to.width, time),
                Mathf.Lerp(from.height, to.height, time));
        }
    }

    class QuatVal : TweenVal<Quaternion>, ITweenVal<Quaternion>
    {
        public QuatVal(Quaternion From, Quaternion To) : base(From, To) { }
        public Quaternion Val(Quaternion From, Quaternion To, float time) { return Quaternion.Slerp(From, To, time); }
    }
}
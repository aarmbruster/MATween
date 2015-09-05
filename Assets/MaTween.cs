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
		internal Coroutine coroutine;

		TweenEngine tweenEngine;
		Easer easer = Ease.FromType(EaseType.Linear);
        T from, to;
        Action<T> Update;
        Action<T> Complete;

        internal Action<float> UpdateTime = (float time) => { };

		public MaTween(T from, T to, float duration, Action<T> Update)
		{
			this.from = from;
			this.to = to;
			this.duration = duration;
			this.easer = Ease.FromType(this.easeType);
			this.Update = Update;
            ITweenVal<T> tweenVal = TweenDelegate(typeof(T).Name);
            this.UpdateTime = (float time) => { Update((T)(object)(tweenVal.Val(tweenVal.From, tweenVal.To, easer(elapsed / duration)))); }; 
			tweenEngine = TweenEngine.Instance;
		}
		
		public MaTween(T from, T to, float duration, Action<T> Update , EaseType easeType) : 				this(from, to, duration, Update) 			{ this.easeType = easeType; }
		public MaTween(T from, T to, float duration, Action<T> Update , float delay) : 						this(from, to, duration, Update) 			{ this.Delay = delay; }
		public MaTween(T from, T to, float duration , Action<T> Update, float delay, EaseType easeType) : 	this(from, to, duration, Update, easeType) 	{ this.easeType = easeType; this.Delay = delay; } 
		
		public void Play ()
		{
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
				tween.elapsed = Mathf.MoveTowards(tween.elapsed, tween.duration, Time.deltaTime);
				yield return new WaitForEndOfFrame();
                tween.UpdateTime(tween.elapsed);
				if(tween.isPaused)
					yield return StartCoroutine(PauseRoutine(tween.coroutine));
				
			}
			if (tween.coroutine != null || tween.willCompleteOnStop)
				tween.UpdateTime (tween.duration);
			
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
        T From{get; set;}
        T To{get; set;}
        T Val(T From, T To, float time);
    }

    class TweenVal<T>
    {
        public T To { get; set; }
        public T From { get; set; }
        public TweenVal(T From, T To)
        {
            this.From = From;
            this.To = To;
        } 
    }

    class FloatVal : TweenVal<float>, ITweenVal<float>
    {
        public FloatVal(float From, float To) : base(From, To) { }
        public float Val (float From, float To, float time) { return Mathf.Lerp(From, To, time); }
    }

    class Vector2Val : TweenVal<Vector2>, ITweenVal<Vector2>
    {
        public Vector2Val(Vector2 From, Vector2 To) : base(From, To) { }
        public Vector2 Val(Vector2 From, Vector2 To, float time) { return Vector2.Lerp(From, To, time); }
    }

    class Vector3Val : TweenVal<Vector3>, ITweenVal<Vector3>
    {
        public Vector3Val(Vector3 From, Vector3 To) : base(From, To) { }
        public Vector3 Val(Vector3 From, Vector3 To, float time) { return Vector3.Lerp(From, To, time); }
    }

    class Vector4Val : TweenVal<Vector4>, ITweenVal<Vector4>
    {
        public Vector4Val(Vector4 From, Vector4 To) : base(From, To) { }
        public Vector4 Val(Vector4 From, Vector4 To, float time) { return Vector4.Lerp(From, To, time); }
    }

    class ColorVal : TweenVal<Color>, ITweenVal<Color>
    {
        public ColorVal(Color From, Color To) : base(From, To) { }
        public Color Val(Color From, Color To, float time) { return Color.Lerp(From, To, time); }
    }

    class RectVal : TweenVal<Rect>, ITweenVal<Rect>
    {
        public RectVal(Rect From, Rect To) : base(From, To) { }
        public Rect Val(Rect From, Rect To, float time) { return new Rect(
                Mathf.Lerp(From.x, To.y, time),
                Mathf.Lerp(From.y, To.y, time),
                Mathf.Lerp(From.width, To.width, time),
                Mathf.Lerp(From.height, To.height, time));
        }
    }

    class QuatVal : TweenVal<Quaternion>, ITweenVal<Quaternion>
    {
        public QuatVal(Quaternion From, Quaternion To) : base(From, To) { }
        public Quaternion Val(Quaternion From, Quaternion To, float time) { return Quaternion.Slerp(From, To, time); }
    }
}
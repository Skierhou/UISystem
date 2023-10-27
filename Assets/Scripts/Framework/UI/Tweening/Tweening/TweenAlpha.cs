using UnityEngine;

/// <summary>
/// Tween the object's alpha. Works with both UI widgets as well as renderers.
/// </summary>

[AddComponentMenu("Tween/Tween Alpha")]
public class TweenAlpha : UITweener
{
	[Range(0f, 1f)] public float from = 1f;
	[Range(0f, 1f)] public float to = 1f;

	[Tooltip("If used on a renderer, the material should probably be cleaned up after this script gets destroyed...")]
	public bool autoCleanup = false;

	[Tooltip("Color to adjust")]
	public string colorProperty;

	[System.NonSerialized] bool mCached = false;
	[System.NonSerialized] CanvasGroup mRect;
	[System.NonSerialized] Material mShared;
	[System.NonSerialized] Material mMat;
	[System.NonSerialized] Light mLight;
	[System.NonSerialized] SpriteRenderer mSr;
	[System.NonSerialized] float mBaseIntensity = 1f;

	[System.Obsolete("Use 'value' instead")]
	public float alpha { get { return this.value; } set { this.value = value; } }

	void OnDestroy () { if (autoCleanup && mMat != null && mShared != mMat) { Destroy(mMat); mMat = null; } }

	void Cache ()
	{
		mCached = true;
		mRect = GetComponent<CanvasGroup>();
		mSr = GetComponent<SpriteRenderer>();

		if (mRect == null && mSr == null)
		{
			mLight = GetComponent<Light>();

			if (mLight == null)
			{
				var ren = GetComponent<Renderer>();

				if (ren != null)
				{
					mShared = ren.sharedMaterial;
					mMat = ren.material;
				}

				if (mMat == null) mRect = GetComponentInChildren<CanvasGroup>();
			}
			else mBaseIntensity = mLight.intensity;
		}
	}

	/// <summary>
	/// Tween's current value.
	/// </summary>

	public float value
	{
		get
		{
			if (!mCached) Cache();
			if (mRect != null) return mRect.alpha;
			if (mSr != null) return mSr.color.a;
			if (mMat == null) return 1f;
			if (string.IsNullOrEmpty(colorProperty)) return mMat.color.a;
			return mMat.GetColor(colorProperty).a;
		}
		set
		{
			if (!mCached) Cache();

			if (mRect != null)
			{
				mRect.alpha = value;
			}
			else if (mSr != null)
			{
				var c = mSr.color;
				c.a = value;
				mSr.color = c;
			}
			else if (mMat != null)
			{
				if (string.IsNullOrEmpty(colorProperty))
				{
					var c = mMat.color;
					c.a = value;
					mMat.color = c;
				}
				else
				{
					var c = mMat.GetColor(colorProperty);
					c.a = value;
					mMat.SetColor(colorProperty, c);
				}
			}
			else if (mLight != null)
			{
				mLight.intensity = mBaseIntensity * value;
			}
		}
	}

	/// <summary>
	/// Tween the value.
	/// </summary>

	protected override void OnUpdate (float factor, bool isFinished) { value = Mathf.Lerp(from, to, factor); }

	/// <summary>
	/// Start the tweening operation.
	/// </summary>

	static public TweenAlpha Begin (GameObject go, float duration, float alpha, float delay = 0f)
	{
		var comp = UITweener.Begin<TweenAlpha>(go, duration, delay);
		comp.from = comp.value;
		comp.to = alpha;

		if (duration <= 0f)
		{
			comp.Sample(1f, true);
			comp.enabled = false;
		}
		return comp;
	}

	public override void SetStartToCurrentValue () { from = value; }
	public override void SetEndToCurrentValue () { to = value; }
}

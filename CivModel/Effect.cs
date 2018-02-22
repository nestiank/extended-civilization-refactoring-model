using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    /// <summary>
    /// Tag of <see cref="Effect"/>. Effects with the same tag cannot be applied together.
    /// </summary>
    public enum EffectTag
    {
        /// <summary>
        /// Tag for ownership related effects.
        /// </summary>
        Ownership
    }

    /// <summary>
    /// Represents an effect to <see cref="Actor"/>.
    /// </summary>
    public abstract class Effect : ITurnObserver
    {
        /// <summary>
        /// The target <see cref="Actor"/> of this effect. <c>null</c> if target was destroyed.
        /// </summary>
        /// <seealso cref="Actor.Destroy"/>
        public Actor Target { get; private set; }

        /// <summary>
        /// <see cref="EffectTag"/> of this effect.
        /// </summary>
        public EffectTag Tag => _tag;
        private readonly EffectTag _tag;

        /// <summary>
        /// The duration turn of this effect.
        /// </summary>
        /// <see cref="LeftTurn"/>
        public int Duration => _duration;
        private readonly int _duration;

        /// <summary>
        /// The left duration turn of this effect.
        /// </summary>
        /// <see cref="Duration"/>
        public int LeftTurn { get; private set; } = -1;

        /// <summary>
        /// Whether this effect is enabled.
        /// </summary>
        /// <remarks>
        /// The setter of this property is a wrapper of <see cref="EffectOn"/> and <see cref="EffectOff"/>.
        /// </remarks>
        /// <seealso cref="EffectOn"/>
        /// <seealso cref="EffectOff"/>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (value && !_enabled)
                    EffectOn();
                else if (!value && _enabled)
                    EffectOff();
            }
        }
        private bool _enabled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Effect"/> class.
        /// </summary>
        /// <param name="target">The target of the effect.</param>
        /// <param name="duration">The duration of the effect. <c>-1</c> if forever.</param>
        /// <param name="tag"><see cref="EffectTag"/> of the effect.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is negative and not -1</exception>
        /// <exception cref="ArgumentNullException"><paramref name="target"/> is <c>null</c>.</exception>
        public Effect(Actor target, int duration, EffectTag tag)
        {
            if (duration < -1)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "duration is negative and not -1");

            Target = target ?? throw new ArgumentNullException(nameof(target));
            _tag = tag;
            _duration = duration;
        }

        /// <summary>
        /// Turn on this effect on <see cref="Target"/> and set <see cref="Enabled"/> to <c>true</c>.
        /// If other <see cref="Effect"/> object with the same <see cref="Tag"/> affects on <see cref="Target"/>, Disable it before turning on this effect.
        /// </summary>
        /// <seealso cref="Enabled"/>
        public void EffectOn()
        {
            if (Enabled)
                throw new InvalidOperationException("effect is already turned on");

            var prevEffect = Target.GetEffectByTag(Tag);
            if (prevEffect != null)
            {
                prevEffect.EffectOff();
            }

            Target.SetEffect(this);
            LeftTurn = Duration;
            _enabled = true;

            OnEffectOn();
        }

        /// <summary>
        /// Turn off this effect on <see cref="Target"/> and set <see cref="Enabled"/> to <c>false</c>.
        /// </summary>
        /// <seealso cref="Enabled"/>
        public void EffectOff()
        {
            if (!Enabled)
                throw new InvalidOperationException("effect is not turned on");

            Target.UnsetEffect(Tag);
            LeftTurn = -1;
            _enabled = false;

            OnEffectOff();
        }

        // this method is used by Actor class
        internal void CallOnTargetDestroy()
        {
            OnTargetDestroy();

            Target = null;
            LeftTurn = -1;
            _enabled = false;
        }

        /// <summary>
        /// Called when <see cref="EffectOn"/> is called. This method should turn on this effect.
        /// </summary>
        protected abstract void OnEffectOn();

        /// <summary>
        /// Called when <see cref="EffectOff"/> is called. This method should turn off this effect.
        /// </summary>
        protected abstract void OnEffectOff();

        /// <summary>
        /// Called when <see cref="Target"/> is destroyed.
        /// </summary>
        protected abstract void OnTargetDestroy();

        /// <summary>
        /// Called after a turn.
        /// </summary>
        public virtual void PostTurn()
        {
            if (Enabled && LeftTurn >= 0)
            {
                if (--LeftTurn <= 0)
                {
                    EffectOff();
                }
            }
        }

        /// <summary>
        /// Called before a turn.
        /// </summary>
        public virtual void PreTurn() { }
        /// <summary>
        /// Called before a sub turn.
        /// </summary>
        /// <param name="playerInTurn">The player which the sub turn is dedicated to.</param>
        public virtual void PrePlayerSubTurn(Player playerInTurn) { }
        /// <summary>
        /// Called after a sub turn.
        /// </summary>
        /// <param name="playerInTurn">The player which the sub turn is dedicated to.</param>
        public virtual void PostPlayerSubTurn(Player playerInTurn) { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CivModel
{
    /// <summary>
    /// The read-only version of <see cref="IActorAction"/>.
    /// </summary>
    public interface IReadOnlyActorAction
    {
        /// <summary>
        /// The <see cref="Actor"/> object which has this action.
        /// </summary>
        Actor Owner { get; }

        /// <summary>
        /// Whether the action has a target parameter or not.
        /// </summary>
        bool IsParametered { get; }

        /// <summary>
        /// Test if the action with given parameter is valid and return required AP to act.
        /// Returns <see cref="ActionPoint.NonAvailable"/> if the action is invalid.
        /// </summary>
        /// <param name="pt">the parameter with which action will be tested.</param>
        /// <returns>
        /// the required AP to act. If the action is invalid, <see cref="ActionPoint.NonAvailable"/>.
        /// </returns>
        ActionPoint GetRequiredAP(Terrain.Point? pt);
    }

    /// <summary>
    /// Represents an action which <see cref="Actor"/> can do.
    /// </summary>
    /// <seealso cref="CivModel.IReadOnlyActorAction" />
    public interface IActorAction : IReadOnlyActorAction
    {
        /// <summary>
        /// Acts with the specified parameter.
        /// </summary>
        /// <param name="pt">The parameter.</param>
        /// <exception cref="ArgumentException">the parameter is invalid.</exception>
        /// <exception cref="InvalidOperationException">Action cannot be done now.</exception>
        void Act(Terrain.Point? pt);
    }

    /// <summary>
    /// Provides a set of static methods for <see cref="IReadOnlyActorAction"/>.
    /// </summary>
    public static class ActorActionExtension
    {
        /// <summary>
        /// Test whether the action is actable with specified parameter.
        /// </summary>
        /// <param name="action">the action.</param>
        /// <param name="pt">the parameter with which action will be tested.</param>
        /// <returns>
        ///   <c>true</c> if the action is actable; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsActable(this IReadOnlyActorAction action, Terrain.Point? pt)
        {
            ActionPoint requiredAP = action.GetRequiredAP(pt);
            return action.Owner.CanConsumeAP(requiredAP);
        }
    }
}

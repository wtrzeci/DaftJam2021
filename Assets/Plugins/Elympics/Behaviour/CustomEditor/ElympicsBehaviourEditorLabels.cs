namespace Elympics
{
	internal partial class ElympicsBehaviourEditor
	{
		private const string Label_BehaviourNotModifiable  = "This behaviour is not modifiable";
		private const string Label_GameObjectSynchronizer  = "GameObject Active";
		private const string Label_TransformSynchronizer   = "Transform";
		private const string Label_RigidbodySynchronizer   = "Rigidbody";
		private const string Label_Rigidbody2DSynchronizer = "Rigidbody2D";
		private const string Label_AnimatorSynchronizer    = "Animator";
		private const string Label_ObservedMonoBehaviours  = "Observed MonoBehaviours:";

		private const string Label_NetworkId = "Network ID:";
		private const string Label_AutoId    = "Auto assign network ID:";

		private const string Label_AutoIdSummary = "Network ids determine the order in which objects are updated";

		private const string Label_AutoIdTooltip = "Enable or disable auto id assignement for this object";

		private const string Label_PredictableFor = "Predictable for: ";

		// TODO: link to docs
		private const string Label_PredictabilitySummary = "Prediction can compensate for network latency and make the game experience smoother.";

		private const string Label_PredictabilityTooltip = "Choose for which players this object will be predicted. Other players will only see updates coming from the server.";

		private const string Label_VisibleFor = "Visible for: ";

		// TODO: link to docs
		private const string Label_VisibilitySummary = "Limiting visibility allows you to synchronise data that should be hidden from other players";

		private const string Label_VisibilityTooltip = "Choose which players will receive data about this object";
	}
}

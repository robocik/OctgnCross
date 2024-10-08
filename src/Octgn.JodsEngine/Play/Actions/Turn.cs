using System.Diagnostics;
using Octgn.UI;

namespace Octgn.Play.Actions
{
    internal sealed class Turn : ActionBase
    {
        private readonly Card _card;
        private readonly bool _up;
        private readonly Player _who;

        public Turn(Player who, Card card, bool up)
        {
            _who = who;
            _card = card;
            _up = up;
        }

        public override void Do()
        {
            base.Do();
            _card.SetFaceUp(_up);
            Program.GameMess.PlayerEvent(_who,"turns '{0}' face {1}", _card, _up ? "up" : "down");

            // Turning an aliased card face up will change its id,
            // which can create bugs if one tries to execute other actions using its current id.
            // That's why scripts have to be suspended until the card is revealed.
            //if (up && card.Type.alias && Script.ScriptEngine.CurrentScript != null)
            //   card.Type.revealSuspendedScript = Script.ScriptEngine.Suspend();
        }
    }
}
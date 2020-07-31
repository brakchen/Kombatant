using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Common.Logging;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Overlay3D;
using ff14bot.Pathing;
using ff14bot.RemoteWindows;
using Kombatant.Enums;
using Kombatant.Extensions;
using Kombatant.Helpers;
using Kombatant.Interfaces;
using LogLevel = ff14bot.Helpers.LogLevel;

namespace Kombatant.Logic
{
    /// <summary>
    /// Logic for autonomous movement functions.
    /// </summary>
    /// <inheritdoc cref="M:Komabatant.Interfaces.LogicExecutor"/>
    internal class Movement : LogicExecutor
    {
        #region Singleton

        private static Movement _movement;
        internal static Movement Instance => _movement ?? (_movement = new Movement());

        #endregion

        private const float MaxDistance = 7.5f;
        private float MaxFlyDisTance = (float)(new Random().Next(15,40));
        private const float MoveToDistance = 7f;
        private Timer timer =new Timer();
        /// <summary>
        /// Main task executor for the Movement logic.
        /// </summary>
        /// <returns>Returns <c>true</c> if any action was executed, otherwise <c>false</c>.</returns>
        internal new async Task<bool> ExecuteLogic()
        {


            if (Settings.BotBase.Instance.IsPaused)
                return await Task.FromResult(false);

            var result = false;

            if(ShouldExecuteAutoMovement())
            {
                if (AvoidanceManager.IsRunningOutOfAvoid)
                    return await Task.FromResult(true);

                switch (Settings.BotBase.Instance.FollowMode)
                {
                    case FollowMode.None:
                        break;

                    case FollowMode.PartyLeader:
                        result = await FollowPartyLeader();
                        break;

                    case FollowMode.FixedCharacter:
                        result = await FollowFixedCharacter();
                        break;

                    case FollowMode.Tank:
                        result = await FollowTank();
                        break;
                }
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Determines whether or not the botbase is allowed to execute movement/follow logics.
        /// </summary>
        /// <returns></returns>
        private bool ShouldExecuteAutoMovement()
        {
            return Settings.BotBase.Instance.FollowMode != FollowMode.None;
        }

        /// <summary>
        /// Core mechanics for the follow logic that are the same across all currently implemented FollowModes.
        /// </summary>
        /// <param name="characterToFollow"></param>
        /// <returns></returns>
        private async Task<bool> PerformFollowLogic(BattleCharacter characterToFollow)
        {
            // Sprint when the leader sprints
            if (PerformAutoSprint(characterToFollow))
                return await Task.FromResult(true);

            // Automatically mount/dismount
            if (await PerformMountDismount(characterToFollow))
                return await Task.FromResult(true);

            // Can't do mount stuff when I am under attack, you silly carbuncle!
            if (Core.Me.InCombat && !Core.Me.IsMounted)
                return await Task.FromResult(false);


            if (await PerformFlightTakeOff(characterToFollow))
                return await Task.FromResult(true);


            if (characterToFollow.Distance2D() > MaxDistance)
            {
                if (await PerformNavigation(characterToFollow))
                    return await Task.FromResult(true);
            }

            Navigator.PlayerMover.MoveStop();

            return await Task.FromResult(false);
        }

        /// <summary>
        /// Follow Mode: Follow Fixed Character
        /// </summary>
        /// <returns></returns>
        private async Task<bool> FollowFixedCharacter()
        {
            if (string.IsNullOrEmpty(Settings.BotBase.Instance.FixedCharacterName))
                return await Task.FromResult(false);

            var fixedCharacter = GameObjectManager.GameObjects
                .FirstOrDefault(obj => obj.Name == Settings.BotBase.Instance.FixedCharacterName &&
                                       obj.IsBattleCharacter());

            // Character not found?
            if(fixedCharacter == null)
                return await Task.FromResult(false);

            return await PerformFollowLogic(fixedCharacter.GetBattleCharacter());
        }

        /// <summary>
        /// Follow Mode: Follow Party Leader
        /// </summary>
        /// <returns></returns>
        private async Task<bool> FollowPartyLeader()
        {
            if (!Core.Me.IsInMyParty())
                return await Task.FromResult(false);

            if (Core.Me.IsPartyLeader())
                return await Task.FromResult(false);

            if (!PartyManager.PartyLeader.IsInObjectManager)
                return await Task.FromResult(false);


            if (SelectYesno.IsOpen)
            {
                SelectYesno.ClickYes();
            }
            var partyLeader = PartyManager.PartyLeader.BattleCharacter;
            
            return await PerformFollowLogic(partyLeader);
        }

        /// <summary>
        /// Follor Mode: Follow Tank
        /// </summary>
        /// <returns></returns>
        private async Task<bool> FollowTank()
        {
            if (!Core.Me.IsInMyParty())
                return await Task.FromResult(false);

            var tankToFollow = PartyManager.VisibleMembers
                .FirstOrDefault(member => member.IsTank())?.BattleCharacter;

            if (tankToFollow == null)
                return await Task.FromResult(false);

            return await PerformFollowLogic(tankToFollow);
        }

        /// <summary>
        /// Determines if the character we are watching has started flying
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private async Task<bool> PerformFlightTakeOff(Character obj)
        {
            
            if (obj.Location.Y >= Core.Me.Location.Y + 5 && !MovementManager.IsFlying)
            {
                await CommonTasks.TakeOff();
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// Tries to navigate to a given GameObject with the selected navigation mode.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private async Task<bool> PerformNavigation(GameObject obj)
        {
            if (Settings.BotBase.Instance.WaypointGenerationMode == WaypointGenerationMode.Offmesh)
            {
                Navigator.PlayerMover.MoveTowards(obj.Location);
                return await Task.FromResult(true);
            }
            Random random = new Random();
            Vector3 walkoffset = new Vector3(random.Next(3,10), random.Next(0,2), random.Next(3,10));
            if (Settings.BotBase.Instance.WaypointGenerationMode == WaypointGenerationMode.NavGraph)
            {

                if (!MovementManager.IsFlying && !MovementManager.IsDiving)
                {

                    await CommonBehaviors.MoveAndStop(
                            r => obj.Location - walkoffset, r => MoveToDistance, true,
                            "Following selected target")
                        .ExecuteCoroutine();
                }
                else
                {
                    if (obj.Distance2D() > MaxFlyDisTance) {
                        Flightor.MoveTo(obj.Location);
                    }
                }

    

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        /// <summary>
        /// Performs auto sprint if necessary.
        /// </summary>
        /// <param name="characterToWatch"></param>
        /// <returns></returns>
        private bool PerformAutoSprint(BattleCharacter characterToWatch)
        {
            if(characterToWatch.HasAura(Constants.Aura.Sprint) && ActionManager.IsSprintReady)
            {
                LogHelper.Instance.Log("[{0}] Sprinting...", CallStackHelper.Instance.GetCaller());
                ActionManager.Sprint();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs automatic mounting/dismounting.
        /// </summary>
        /// <param name="characterToWatch"></param>
        /// <returns></returns>
        private async Task<bool> PerformMountDismount(BattleCharacter characterToWatch)
        {
            if (characterToWatch.IsMounted != Core.Me.IsMounted || characterToWatch.IsCasting && characterToWatch.CastingSpellId == Constants.Action.Mount)
            {
                // Mount up!
                if (characterToWatch.IsMounted || characterToWatch.CastingSpellId == Constants.Action.Mount)
                {
                    LogHelper.Instance.Log(
                        "[{0}] Mounting...",
                        CallStackHelper.Instance.GetCaller());
                    await Coroutine.Sleep(new Random().Next(2,5)*1000);
                    await CommonTasks.MountUp();
                    return await Task.FromResult(true);
                }

                // Dismount - but only when close to the leader!
                if(characterToWatch.Distance2D() <= MaxDistance)
                {
                    LogHelper.Instance.Log(
                        "[{2}] Dismounting, {0} <= {1}...",
                        characterToWatch.Distance2D(), MaxDistance, CallStackHelper.Instance.GetCaller());
                    await CommonTasks.StopAndDismount();
                    return await Task.FromResult(true);
                }
            }

            return await Task.FromResult(false);
        }
    }
}
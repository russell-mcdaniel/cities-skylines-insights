﻿using System;
using ICities;
using Insights.Game.Events;
using Insights.Logging;

namespace Insights.Game.Extensions
{
    public class LoadingExtension : LoadingExtensionBase
    {
        protected InsightsLogger Logger { get; } = new InsightsLogger(typeof(LoadingExtension));

        protected SessionContext Context { get; } = SessionContext.Instance;

        /// <summary>
        /// Occurs when one of the main game modes is entered.
        /// </summary>
        /// <remarks>
        /// This is called when entering one of the game modes from the main menu. This includes
        /// the game itself or one of the editors (asset, map, scenario, or theme).
        /// 
        /// It is not called from within an active session. For example, loading a different map
        /// while in the game does not call this method.
        /// </remarks>
        public override void OnCreated(ILoading loading)
        {
            Logger.LogDebug($"OnCreated > CurrentMode: {loading.currentMode} | CurrentTheme: {loading.currentTheme} | LoadingComplete: {loading.loadingComplete}");

            base.OnCreated(loading);
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            Logger.LogDebug($"OnLevelLoaded > LoadMode: {mode}");

            // Create and preserve ID for new session.
            Context.SessionId = Guid.NewGuid();

            var manager = SimulationManager.instance;

            // Create the Session Begin event.
            var @event = new SessionStartedEvent
            {
                SessionTime = DateTimeOffset.Now,
                SessionId = Context.SessionId,
                GameTime = manager.m_currentGameTime,
                EventType = EventType.SessionStarted,
                Type = managers.loading.currentMode,
                // Is this the same subtype provided by LoadingManager.LevelLoaded?
                Subtype = manager.m_metaData.m_updateMode,
                InstanceId = new Guid(manager.m_metaData.m_gameInstanceIdentifier),
                CityName = manager.m_metaData.m_CityName
            };

            Logger.LogEvent(@event);

            base.OnLevelLoaded(mode);
        }

        public override void OnLevelUnloading()
        {
            Logger.LogDebug("OnLevelUnloading");

            var manager = SimulationManager.instance;

            var @event = new SessionEndedEvent
            {
                SessionTime = DateTimeOffset.Now,
                SessionId = Context.SessionId,
                GameTime = manager.m_currentGameTime,
                EventType = EventType.SessionEnded
            };

            Logger.LogEvent(@event);

            base.OnLevelUnloading();
        }

        public override void OnReleased()
        {
            Logger.LogDebug("OnReleased");

            base.OnReleased();
        }
    }
}

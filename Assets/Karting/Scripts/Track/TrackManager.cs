using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KartGame.KartSystems;
using UnityEngine;

namespace KartGame.Track {
    /// <summary>
    /// A MonoBehaviour to deal with all the time and positions for the racers.
    /// </summary>
    public class TrackManager : MonoBehaviour {
        [Tooltip ("The name of the track in this scene.  Used for track time records.  Must be unique.")]
        public string trackName;
        [Tooltip ("Number of laps for the race.")]
        public int raceLapTotal;
       // [Tooltip ("All the checkpoints for the track in the order that they should be completed starting with the start/finish line checkpoint.")]
       // public List<Checkpoint> checkpoints = new List<Checkpoint> ();
        [Tooltip ("Reference to an object responsible for repositioning karts.")]
        public KartRepositioner kartRepositioner;

        bool m_IsStart;
        Dictionary<IRacer, Checkpoint> m_RacerNextCheckpoints = new Dictionary<IRacer, Checkpoint> (16);
        TrackRecord m_SessionBestLap = TrackRecord.CreateDefault ();
        TrackRecord m_SessionBestRace = TrackRecord.CreateDefault ();
        TrackRecordData m_trackRecordData = TrackRecordData.CreateHistory ();

        public bool IsStart => m_IsStart;

        /// <summary>
        /// Returns the best lap time recorded this session.  If no record is found, -1 is returned.
        /// </summary>
        public float SessionBestLap {
            get {
                if (m_SessionBestLap != null && m_SessionBestLap.time < float.PositiveInfinity)
                    return m_SessionBestLap.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best race time recorded this session.  If no record is found, -1 is returned.
        /// </summary>
        public float SessionBestRace {
            get {
                if (m_SessionBestRace != null && m_SessionBestRace.time < float.PositiveInfinity)
                    return m_SessionBestRace.time;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best lap time ever recorded.  If no record is found, -1 is returned.
        /// </summary>
        public float HistoricalBestLap {
            get {
                // if (m_HistoricalBestLap != null && m_HistoricalBestLap.time < float.PositiveInfinity)
                //     return m_HistoricalBestLap.time;
                if (m_trackRecordData != null && m_trackRecordData.bestLapTime < float.PositiveInfinity)
                    return m_trackRecordData.bestLapTime;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the best race time ever recorded.  If no record is found, -1 is returned.
        /// </summary>
        public float HistoricalBestRace {
            get {
                // if (m_HistoricalBestRace != null && m_HistoricalBestRace.time < float.PositiveInfinity)
                //     return m_HistoricalBestRace.time;
                if (m_trackRecordData != null && m_trackRecordData.bestRaceTime < float.PositiveInfinity)
                    return m_trackRecordData.bestRaceTime;
                return -1f;
            }
        }

        /// <summary>
        /// Returns the rank of Player.
        /// </summary>
        public string GamerRank {
            get {
                if(m_trackRecordData != null)
                    return Convert.ToString(m_trackRecordData.rank);
                return "---";
            }
        }

        void Awake () {
    
        }

        void OnEnable () {
    
        }

        void OnDisable () {

        }

        void Start () { }

        /// <summary>
        /// Starts the timers and enables control of all racers.
        /// </summary>
        public void StartRace () {
 

          //  m_RacerNextCheckpoints.Clear ();

            UnityEngine.Object[] allRacerArray = FindObjectsOfType<UnityEngine.Object> ().Where (x => x is IControllable).ToArray ();

            for (int i = 0; i < allRacerArray.Length; i++) {
                IControllable racer = allRacerArray[i] as IControllable;
                racer.DisableControl ();
            }

            m_IsStart = true;

            for (int i = 0; i < allRacerArray.Length; i++)
            {
                IControllable racer = allRacerArray[i] as IControllable;
                racer.EnableControl();
            }

            //foreach (KeyValuePair<IControllable, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints) {
            //    racerNextCheckpoint.Key.EnableControl ();
            //    racerNextCheckpoint.Key.UnpauseTimer ();
            //}

            Debug.Log("3)游戏开始，可以发送、接受帧消息了");
            UI.MetaGameController.Ins.isStarted = true;
            Client.Ins.StartFrameSync();
        }

        /// <summary>
        /// Stops the timers and disables control of all racers, also saves the historical records.
        /// </summary>
        public void StopRace () {
            m_IsStart = false;

            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints) {
                racerNextCheckpoint.Key.DisableControl ();
                racerNextCheckpoint.Key.PauseTimer ();
            }

            var record = new TrackRecordData {
                bestLapTime = m_SessionBestLap.time,
                bestRaceTime = m_SessionBestRace.time
            };
            TrackRecordData.UploadRecord (record);

            // TrackRecord.Save (m_HistoricalBestLap);
            // TrackRecord.Save (m_HistoricalBestRace);
        }

        void CheckRacerHitCheckpoint (IRacer racer, Checkpoint checkpoint) {
            //if (!m_IsStart) {
            //    StartCoroutine (CallWhenRaceStarts (racer, checkpoint));
            //    return;
            //}

            //if (m_RacerNextCheckpoints.ContainsKeyValuePair (racer, checkpoint)) {
            //    m_RacerNextCheckpoints[racer] = checkpoints.GetNextInCycle (checkpoint);
            //    RacerHitCorrectCheckpoint (racer, checkpoint);
            //} else {
            //    RacerHitIncorrectCheckpoint (racer, checkpoint);
            //}
        }

        IEnumerator CallWhenRaceStarts (IRacer racer, Checkpoint checkpoint) {
            while (!m_IsStart) {
                yield return null;
            }

            CheckRacerHitCheckpoint (racer, checkpoint);
        }

        void RacerHitCorrectCheckpoint (IRacer racer, Checkpoint checkpoint) {
            if (checkpoint.isStartFinishLine) {
                int racerCurrentLap = racer.GetCurrentLap ();
                if (racerCurrentLap > 0) {
                    float lapTime = racer.GetLapTime ();

                    if (m_SessionBestLap.time > lapTime)
                        m_SessionBestLap.SetRecord (trackName, 1, racer, lapTime);

                    if (m_trackRecordData.bestLapTime > lapTime)
                        // m_HistoricalBestLap.SetRecord (trackName, 1, racer, lapTime);
                        m_trackRecordData.bestLapTime = lapTime;

                    if (racerCurrentLap == raceLapTotal) {
                        float raceTime = racer.GetRaceTime ();

                        if (m_SessionBestRace.time > raceTime)
                            m_SessionBestRace.SetRecord (trackName, raceLapTotal, racer, raceTime);

                        // if (m_HistoricalBestRace.time > raceTime)
                        //     m_HistoricalBestLap.SetRecord (trackName, raceLapTotal, racer, raceTime);
                        if (m_trackRecordData.bestRaceTime > raceTime)
                            // m_HistoricalBestLap.SetRecord (trackName, 1, racer, lapTime);
                            m_trackRecordData.bestRaceTime = raceTime;

                        racer.DisableControl ();
                        racer.PauseTimer ();
                    }
                }

                if (CanEndRace ())
                    StopRace ();

                racer.HitStartFinishLine ();
            }
        }

        bool CanEndRace () {
            foreach (KeyValuePair<IRacer, Checkpoint> racerNextCheckpoint in m_RacerNextCheckpoints) {
                if (racerNextCheckpoint.Key.GetCurrentLap () < raceLapTotal)
                    return false;
            }

            return true;
        }

        void RacerHitIncorrectCheckpoint (IRacer racer, Checkpoint checkpoint) {
            // No implementation required by template.
        }

        /// <summary>
        /// This function should be called when a kart gets stuck or falls off the track.
        /// It will find the last checkpoint the kart went through and reposition it there.
        /// </summary>
        /// <param name="movable">The movable representing the kart.</param>
        public void ReplaceMovable (IMovable movable) {
            //IRacer racer = movable.GetRacer ();

            //if (racer == null)
            //    return;

            //Checkpoint nextCheckpoint = m_RacerNextCheckpoints[racer];
            //int lastCheckpointIndex = (checkpoints.IndexOf (nextCheckpoint) + checkpoints.Count - 1) % checkpoints.Count;
            //Checkpoint lastCheckpoint = checkpoints[lastCheckpointIndex];

            //bool isControlled = movable.IsControlled ();
            //movable.DisableControl ();
            //kartRepositioner.OnRepositionComplete += ReenableControl;

            //kartRepositioner.Reposition (lastCheckpoint, movable, isControlled);
        }

        void ReenableControl (IMovable movable, bool doEnableControl) {
            if (doEnableControl)
                movable.EnableControl ();
            kartRepositioner.OnRepositionComplete -= ReenableControl;
        }
    }
}
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

namespace KartGame.Track {
    /// <summary>
    /// A serializable record for the time on a track.
    /// </summary>
    public class TrackRecord {
        /// <summary>
        /// The name of the track this record belongs to.
        /// </summary>
        public string trackName;
        /// <summary>
        /// The number of laps this record is for.
        /// </summary>
        public int laps;
        /// <summary>
        /// The time of this record.
        /// </summary>
        public float time;
        /// <summary>
        /// The name of the racer who recorded this time.
        /// </summary>
        public string name;
        /// <summary>
        /// The rank of this record 
        /// </summary>
        [SerializeField]
        public int rank;
        const float k_DefaultTime = float.PositiveInfinity;
        const string k_FolderName = "BinaryTrackRecordData";
        const string k_FileExtension = ".dat";

        /// <summary>
        /// Set all the information in a record.
        /// </summary>
        /// <param name="track">The new name of the track.</param>
        /// <param name="lapCount">The new lap count.</param>
        /// <param name="racer">The new racer whose name will be recorded.</param>
        /// <param name="newTime">The new time for the record.</param>
        public void SetRecord (string track, int lapCount, IRacer racer, float newTime) {
            trackName = track;
            laps = lapCount;
            name = racer.GetName ();
            time = newTime;
        }

        /// <summary>
        /// Creates a TrackRecord with default values.
        /// </summary>
        public static TrackRecord CreateDefault () {
            TrackRecord defaultRecord = new TrackRecord ();
            defaultRecord.time = k_DefaultTime;
            return defaultRecord;
        }

        /// <summary>
        /// Saves a record using a file name based on the track name and number of laps.
        /// </summary>
        public static void Save (TrackRecord record) {
            // string folderPath = Path.Combine (Application.persistentDataPath, k_FolderName);

            // if (!Directory.Exists (folderPath))
            //     Directory.CreateDirectory (folderPath);

            // string dataPath = Path.Combine (folderPath, record.trackName + record.laps + k_FileExtension);

            // BinaryFormatter binaryFormatter = new BinaryFormatter ();

            // using (FileStream fileStream = File.Open (dataPath, FileMode.OpenOrCreate)) {
            //     binaryFormatter.Serialize (fileStream, record);
            // }
        }

        /// <summary>
        /// Finds and loads a TrackRecord file.
        /// </summary>
        /// <param name="track">The name of the track to be loaded.</param>
        /// <param name="lapCount">The number of laps of the record to be loaded.</param>
        /// <returns>The loaded record.</returns>
        public static TrackRecord Load (string track, int lapCount) {
            string folderPath = Path.Combine (Application.persistentDataPath, k_FolderName);

            if (!Directory.Exists (folderPath))
                Directory.CreateDirectory (folderPath);

            string dataPath = Path.Combine (folderPath, track + lapCount + k_FileExtension);

            BinaryFormatter binaryFormatter = new BinaryFormatter ();

            using (FileStream fileStream = File.Open (dataPath, FileMode.OpenOrCreate)) {
                if (fileStream.Length == 0)
                    return CreateDefault ();

                try {
                    TrackRecord loadedRecord = binaryFormatter.Deserialize (fileStream) as TrackRecord;

                    if (loadedRecord == null)
                        return CreateDefault ();
                    return loadedRecord;
                } catch (Exception) {
                    return CreateDefault ();
                }
            }
        }
    }

    [Serializable]
    public class TrackRecordData : ISerializable {
        public float bestLapTime;
        public float bestRaceTime;
        public int rank;
        public string name;
        public string mgobeId;
        public float bestHistoryTime;
        public TrackRecordData () {

        }

        /// <summary>
        /// Create a TrackRecordDate with history values.
        /// </summary>
        /// <returns></returns>
        public static TrackRecordData CreateHistory () {
            var task = Task<TrackRecord>.Run (CloudBaseClient.GetHistoryRecord);
            return task.Result;
        }

        /// <summary>
        /// Upload Gamer's trackRecordData
        /// </summary>
        /// <param name="recordData"></param>
        public static void UploadRecord (TrackRecordData recordData) {
            var task = Task.Run (() => CloudBaseClient.UploadRecord (recordData));
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context) {
            info.AddValue ("name", name);
            info.AddValue ("mgobeId", mgobeId);
            info.AddValue ("rank", rank);
            info.AddValue ("bestLapTime", bestLapTime);
            info.AddValue ("bestRaceTime", bestRaceTime);
        }

        protected TrackRecordData (SerializationInfo info, StreamingContext context) {
            name = info.GetString ("name");
            bestLapTime =  (float)info.GetDouble ("bestLapTime");
            bestRaceTime = (float)info.GetDouble  ("bestRaceTime");
            rank = info.GetInt32 ("rank");
        }
    }
}
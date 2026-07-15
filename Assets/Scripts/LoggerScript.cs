using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Logging
{
    public class LoggerScript : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("If true, logging starts automatically when the game begins. Set FALSE if controlled by DartboardLogic.")]
        public bool autoStartLogging = false;

        [Header("XR Elements to Log")]
        public Transform headCamera;       
        public Transform leftHand;         
        public Transform rightHand;        

        [Header("Objects to Log")]
        public List<string> objectTagsToLog = new List<string>() { "Dart" };

        private readonly string fileNamePrefix = "DartGame_Log";
        private string basePath = "";
        private const char Delim = '\t'; 
        private StreamWriter _streamWriter;
        private readonly List<string> _eventsTriggered = new List<string>();
        private readonly DateTime _epochStart = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        private bool _isLogging;

        private List<Transform> _trackedObjectTransforms = new List<Transform>();

        void Start()
        {
            basePath = Application.persistentDataPath + "/DataLogs/";
            if (autoStartLogging) StartDefaultSession();
        }

        void Update()
        {
            if (_isLogging && _streamWriter != null)
            {
                var timestamp = (DateTime.UtcNow - _epochStart).TotalMilliseconds;
                var events = _eventsTriggered.Count > 0 ? string.Join("|", _eventsTriggered) : "None";
                
                // 1. Common Data String
                var baseRow = $"{Time.frameCount}{Delim}{Time.realtimeSinceStartup}{Delim}{timestamp}{Delim}{events}{Delim}";
                baseRow += LogTransform(headCamera);
                baseRow += LogTransform(leftHand);
                baseRow += LogTransform(rightHand);

                // 2. Write Rows
                if (_trackedObjectTransforms.Count > 0)
                {
                    foreach (var obj in _trackedObjectTransforms)
                    {
                        if (obj != null)
                        {
                            // IMPORTANT: This structure [Base] + [Name] + [Transform]
                            // MUST match the Header structure exactly.
                            string finalRow = baseRow + $"{obj.name}{Delim}" + LogTransform(obj);
                            _streamWriter.WriteLine(finalRow);
                        }
                    }
                }
                else
                {
                    string finalRow = baseRow + $"None{Delim}" + LogEmpty();
                    _streamWriter.WriteLine(finalRow);
                }
                
                _eventsTriggered.Clear();
            }
        }

        private string LogTransform(Transform t)
        {
            if (t == null) return LogEmpty();
            Vector3 p = t.position;
            Vector3 r = t.eulerAngles;
            Quaternion q = t.rotation;
            // FIXED: Added missing {Delim} between y/z components for Position, Rotation, and Quaternion
            return $"{p.x:F4}{Delim}{p.y:F4}{Delim}{p.z:F4}{Delim}{r.x:F4}{Delim}{r.y:F4}{Delim}{r.z:F4}{Delim}{q.x:F4}{Delim}{q.y:F4}{Delim}{q.z:F4}{Delim}{q.w:F4}{Delim}";
        }

        private string LogEmpty()
        {
            // 10 Empty Columns
            return $"{Delim}{Delim}{Delim}{Delim}{Delim}{Delim}{Delim}{Delim}{Delim}{Delim}";
        }

        public void StartDefaultSession() { StartLogging(1, 1, "Training", 0); }

        public void StartLogging(int participantId, int sessionId, string condition, int handUsed)
        {
            if (_isLogging) return;

            _trackedObjectTransforms.Clear();
            foreach (var tag in objectTagsToLog)
            {
                GameObject[] found = GameObject.FindGameObjectsWithTag(tag);
                foreach (var go in found)
                {
                    if (!_trackedObjectTransforms.Contains(go.transform))
                        _trackedObjectTransforms.Add(go.transform);
                }
            }

            string fileInfo = $"P{participantId}_S{sessionId}_{condition}_Hand{handUsed}";
            string fileName = $"{fileNamePrefix}_{fileInfo}_{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.tsv";
            if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);
            
            _streamWriter = new StreamWriter(Path.Combine(basePath, fileName));
            
            // Write Header
            _streamWriter.WriteLine(CreateHeader());
            _streamWriter.Flush();

            _isLogging = true;
            AddEvent("SessionStarted");
        }

        public void StopLogging()
        {
            if (!_isLogging) return;
            _isLogging = false;
            if (_streamWriter != null) { _streamWriter.Flush(); _streamWriter.Close(); _streamWriter = null; }
        }

        public void AddEvent(string eventName) { if (_isLogging) _eventsTriggered.Add(eventName); }

        private string CreateHeader()
        {
            // 1. Common Headers
            string h = $"Frame{Delim}Time{Delim}Timestamp{Delim}Events{Delim}";
            
            // Head (10 cols)
            h += "Head_Px{Delim}Head_Py{Delim}Head_Pz{Delim}Head_Rx{Delim}Head_Ry{Delim}Head_Rz{Delim}Head_Qx{Delim}Head_Qy{Delim}Head_Qz{Delim}Head_Qw{Delim}";
            
            // Left Hand (10 cols)
            h += "Left_Px{Delim}Left_Py{Delim}Left_Pz{Delim}Left_Rx{Delim}Left_Ry{Delim}Left_Rz{Delim}Left_Qx{Delim}Left_Qy{Delim}Left_Qz{Delim}Left_Qw{Delim}";
            
            // Right Hand (10 cols)
            h += "Right_Px{Delim}Right_Py{Delim}Right_Pz{Delim}Right_Rx{Delim}Right_Ry{Delim}Right_Rz{Delim}Right_Qx{Delim}Right_Qy{Delim}Right_Qz{Delim}Right_Qw{Delim}";

            // 2. Object Headers (11 Cols: Name + 10 Transform)
            // This is the generic header for the "Current Row's Object"
            h += "Object_Name{Delim}Obj_Px{Delim}Obj_Py{Delim}Obj_Pz{Delim}Obj_Rx{Delim}Obj_Ry{Delim}Obj_Rz{Delim}Obj_Qx{Delim}Obj_Qy{Delim}Obj_Qz{Delim}Obj_Qw";

            // Important: Replace {Delim} placeholder with actual Tab char
            return h.Replace("{Delim}", "\t");
        }
    }
}
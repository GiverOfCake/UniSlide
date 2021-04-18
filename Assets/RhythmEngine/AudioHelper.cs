using System.Collections;
using System.Collections.Generic;
using RhythmEngine.Model;
using UnityEngine;
using UnityEngine.Networking;

namespace RhythmEngine
{
	public class AudioHelper
	{
		private static double MachineTimingOffset = 0.0f;

		private static AudioType AudioTypeFromFilename(string filename)
		{
			filename = filename.ToLower();
			if (filename.EndsWith(".mp3") || filename.EndsWith(".mp2"))
				return AudioType.MPEG;
			if (filename.EndsWith(".ogg"))
				return AudioType.OGGVORBIS;
			if (filename.EndsWith(".wav"))
				return AudioType.WAV;
			Debug.LogError($"Unknown/unsupported audio format from file: {filename}");
			return AudioType.UNKNOWN;
		}

		public static IEnumerator LoadAudio(AudioSource source, Song song)
		{
			var audioType = AudioTypeFromFilename(song.AudioFile);
			using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + song.Directory + "\\" + song.AudioFile, audioType))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError)
				{
					Debug.Log(www.error);
				}
				else
				{
					var audioClip = DownloadHandlerAudioClip.GetContent(www);
					source.clip = audioClip;
					while (source.clip.loadState != AudioDataLoadState.Loaded)
						yield return new WaitForSeconds(0.1f);
					//source.Play();
				}
			}
		}
	}
}
using System;
using UnityEngine;

namespace ZellyDevGames.Audio
{
    [Serializable]
    public class SFXEntry
    {
        public string key;
        public AudioClip clip;
        public float spatialBlend;
        public float volume;
        public float simultaneousInstanceLimit;
    }
}
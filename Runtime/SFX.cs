using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZellyDevGames.Monoservice;

namespace ZellyDevGames.Audio
{
    [Service]
    public class SFX : MonoBehaviour
    {
        private static SFX _instance;

        [SerializeField] private int audioSourcePoolSize;
        [SerializeField] private int globalClipLimit;
        
        private readonly Dictionary<string, int> _clipSemaphores = new();
        private Stack<AudioPoolMember> _audioSourcePool;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _audioSourcePool = new Stack<AudioPoolMember>(audioSourcePoolSize);
            for (var i = 0; i < audioSourcePoolSize; i++)
            {
                var s = new GameObject($"SFXSource_{i}", typeof(AudioSource));
                s.transform.SetParent(transform);
                _audioSourcePool.Push(new AudioPoolMember(s.GetComponent<AudioSource>(), s));
            }
        }

        
        public void Play2D(SFXEntry entry, float volume = -1f)
        {
            if (!AcquireSourceFromPool(entry, out var audioPoolMember)) return;
            
            audioPoolMember.Source.clip = entry.clip;
            audioPoolMember.Source.spatialBlend = 0;
            audioPoolMember.Source.volume = volume == -1f ? entry.volume : volume;
            audioPoolMember.Source.Play();
            StartCoroutine(ReturnSourceToPool(audioPoolMember, entry));
        }

        public void Play3D(SFXEntry entry, Vector3 position, float volume = -1f)
        {
            if (!AcquireSourceFromPool(entry, out var audioPoolMember)) return;
            
            audioPoolMember.Source.clip = entry.clip;
            audioPoolMember.Source.volume = volume == -1f ? entry.volume : volume;
            audioPoolMember.Source.spatialBlend = entry.spatialBlend;
            audioPoolMember.SourceObject.transform.position = position;
            audioPoolMember.Source.Play();
            StartCoroutine(ReturnSourceToPool(audioPoolMember, entry));
        }
        
        private bool AcquireSourceFromPool(SFXEntry entry, out AudioPoolMember audioPoolMember)
        {
            audioPoolMember = null;
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"SFX entry or entry clip null {entry?.key}");
                return false;
            }
            
            if (_clipSemaphores.TryGetValue(entry.key, out var semaphore))
            {
                if (
                    (entry.simultaneousInstanceLimit > 0 && semaphore >= entry.simultaneousInstanceLimit) ||
                    (globalClipLimit > 0 && semaphore >= globalClipLimit))
                {
                    return false;
                }
                
                _clipSemaphores[entry.key]++;
            }
            else
            {
                _clipSemaphores[entry.key] = 1;
            }
            
            if (!_audioSourcePool.TryPop(out var fromPool))
            {
                _clipSemaphores[entry.key]--;
                if (_clipSemaphores[entry.key] <= 0) _clipSemaphores.Remove(entry.key);
                return false;
            }
            audioPoolMember = fromPool;
            return true;
        }

        private IEnumerator ReturnSourceToPool(AudioPoolMember member, SFXEntry entry)
        {
            yield return new WaitWhile(() => member.Source.isPlaying);

            _clipSemaphores[entry.key]--;
            if (_clipSemaphores[entry.key] <= 0) _clipSemaphores.Remove(entry.key);

            ResetSource(member);
            _audioSourcePool.Push(member);
        }
        
        private static void ResetSource(AudioPoolMember member)
        {
            member.SourceObject.transform.position = Vector3.zero;
            member.Source.Stop();
            member.Source.clip = null;
            member.Source.spatialBlend = 0f;
            member.Source.volume = 1f;
            member.Source.pitch = 1f;
            member.Source.loop = false;
        }
    }

    public class AudioPoolMember
    {
        public readonly AudioSource Source;
        public readonly GameObject SourceObject;

        public AudioPoolMember(AudioSource s, GameObject go)
        {
            Source = s;
            SourceObject = go;
        }
    }
}
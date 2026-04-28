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

        
        public AudioPoolMember Play2D(SFXEntry entry, float volume = -1f)
        {
            if (!AcquireSourceFromPool(entry, out var audioPoolMember)) return null;
            
            audioPoolMember.Source.loop = entry.loop;
            audioPoolMember.Source.clip = entry.clip;
            audioPoolMember.Entry = entry;
            audioPoolMember.Source.spatialBlend = 0;
            audioPoolMember.Source.volume = volume == -1f ? entry.volume : volume;
            audioPoolMember.Source.Play();
            audioPoolMember.AutoStop = !entry.loop;
            if (!entry.loop)
            {
                StartCoroutine(ReturnSourceToPool(audioPoolMember));
            }
            return audioPoolMember;
        }

        public AudioPoolMember Play3D(SFXEntry entry, Vector3 position, float volume = -1f)
        {
            if (!AcquireSourceFromPool(entry, out var audioPoolMember)) return null;
            
            audioPoolMember.Source.clip = entry.clip;
            audioPoolMember.Source.loop = entry.loop;
            audioPoolMember.Entry = entry;
            audioPoolMember.Source.volume = volume == -1f ? entry.volume : volume;
            audioPoolMember.Source.spatialBlend = entry.spatialBlend;
            audioPoolMember.SourceObject.transform.position = position;
            audioPoolMember.Source.Play();
            audioPoolMember.AutoStop = !entry.loop;
            if (!entry.loop)
            {
                StartCoroutine(ReturnSourceToPool(audioPoolMember));
            }
            
            return audioPoolMember;
        }
        
        public void Stop(AudioPoolMember member)
        {
            if (member == null || member.Entry == null || member.AutoStop) return;
            member.Source.Stop();
            ReleaseMemberResource(member);
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

        private IEnumerator ReturnSourceToPool(AudioPoolMember member)
        {
            yield return new WaitWhile(() => member.Source.isPlaying);
            ReleaseMemberResource(member);
        }

        private void ReleaseMemberResource(AudioPoolMember member)
        {
            _clipSemaphores[member.Entry.key]--;
            if (_clipSemaphores[member.Entry.key] <= 0) _clipSemaphores.Remove(member.Entry.key);

            ResetSource(member);
            _audioSourcePool.Push(member);
        }
        
        private static void ResetSource(AudioPoolMember member)
        {
            member.SourceObject.transform.localPosition = Vector3.zero;
            member.Source.Stop();
            member.Source.clip = null;
            member.Source.spatialBlend = 0f;
            member.Source.volume = 1f;
            member.Source.pitch = 1f;
            member.Source.loop = false;
            member.Entry = null;
            member.AutoStop = false;
        }
    }

    public class AudioPoolMember
    {
        public readonly AudioSource Source;
        public readonly GameObject SourceObject;
        public SFXEntry Entry;
        public bool AutoStop;

        public AudioPoolMember(AudioSource s, GameObject go)
        {
            Source = s;
            SourceObject = go;
        }
    }
}
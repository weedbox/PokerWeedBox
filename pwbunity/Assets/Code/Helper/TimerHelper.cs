using System.Collections;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Code.Helper
{
    public class TimerHelper
    {
        private readonly MonoBehaviour _monoBehaviour;
        private float _inFuture;
        private float _untilFinished;
        private UnityAction<float> _countDownCallback;
        private UnityAction _endCallback;

        [CanBeNull] private Coroutine _currentCoroutine;

        public TimerHelper(MonoBehaviour value)
        {
            _monoBehaviour = value;
        }

        public void StartTimer(float future, UnityAction callback)
        {
            _inFuture = future;
            _endCallback = callback;

            StopTimer();
            if (_monoBehaviour.IsUnityNull()) return;
            _currentCoroutine = _monoBehaviour.StartCoroutine(Routine());
        }

        public void StartCountDown(float future, float interval, UnityAction<float> countDownCallback,
            UnityAction endCallback)
        {
            _inFuture = future;
            _untilFinished = interval;
            _endCallback = endCallback;
            _countDownCallback = countDownCallback;

            StopTimer();
            if (_monoBehaviour.IsUnityNull()) return;
            _currentCoroutine = _monoBehaviour.StartCoroutine(CountDownRoutine());
        }

        public void StopTimer()
        {
            if (_monoBehaviour.IsUnityNull() || _currentCoroutine == null) return;
            _monoBehaviour.StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        private IEnumerator Routine()
        {
            yield return new WaitForSeconds(_inFuture);
            _endCallback?.Invoke();
        }

        private IEnumerator CountDownRoutine()
        {
            while (_inFuture > 0)
            {
                yield return new WaitForSeconds(_untilFinished);
                _inFuture -= _untilFinished;
                _countDownCallback?.Invoke(_inFuture);
            }

            _endCallback?.Invoke();
        }
    }
}
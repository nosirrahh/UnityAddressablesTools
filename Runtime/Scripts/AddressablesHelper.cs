using NosirrahhTools.UnityCoreTools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace NosirrahhTools.UnityAddressablesTools
{
    /// <summary>
    /// Helper class for managing Unity Addressables operations.
    /// </summary>
    public class AddressablesHelper : Singleton<AddressablesHelper>
    {
        #region Public Methods

        /// <summary>
        /// Loads an Addressable asset asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="addressableName">The name or key of the Addressable asset.</param>
        /// <param name="onCompleted">Callback invoked upon operation completion, with the status and the loaded asset.</param>
        /// <param name="onProgressUpdated">Optional callback invoked to report loading progress (percentage).</param>
        public void Load<T> (string addressableName, UnityAction<AsyncOperationStatus, T> onCompleted, UnityAction<float> onProgressUpdated = null)
        {
            StartCoroutine (LoadCoroutine (addressableName, onCompleted, onProgressUpdated));
        }

        /// <summary>
        /// Checks if an Addressable asset exists.
        /// </summary>
        /// <typeparam name="T">The type of the asset to check.</typeparam>
        /// <param name="addressableName">The name or key of the Addressable asset.</param>
        /// <param name="onCompleted">Callback invoked with the operation status indicating success or failure.</param>
        public void Exists<T> (string addressableName, UnityAction<AsyncOperationStatus> onCompleted)
        {
            StartCoroutine (ExistsCoroutine<T> (addressableName, onCompleted));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine for loading an Addressable asset.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="addressableName">The name or key of the Addressable asset.</param>
        /// <param name="onCompleted">Callback invoked upon operation completion, with the status and the loaded asset.</param>
        /// <param name="onProgressUpdated">Optional callback invoked to report loading progress (percentage).</param>
        /// <returns>An enumerator for coroutine execution.</returns>
        private IEnumerator LoadCoroutine<T> (string addressableName, UnityAction<AsyncOperationStatus, T> onCompleted, UnityAction<float> onProgressUpdated = null)
        {
            AsyncOperationHandle<object> asyncOperationHandle = Addressables.LoadAssetAsync<object> (addressableName);

            yield return new WaitWhile (
                () =>
                {
                    if (!asyncOperationHandle.IsDone)
                    {
                        DownloadStatus downloadStatus = asyncOperationHandle.GetDownloadStatus ();
                        onProgressUpdated?.Invoke (downloadStatus.Percent);
                    }
                    return !asyncOperationHandle.IsDone;
                }
            );

            try
            {
                onCompleted?.Invoke (asyncOperationHandle.Status, (T)asyncOperationHandle.Result);
            }
            catch (Exception exception)
            {
                Debug.LogError ($"[{nameof(AddressablesHelper)}] {nameof(Load)} - Exception: {exception}");
                onCompleted?.Invoke (AsyncOperationStatus.Failed, default);
            }
        }

        /// <summary>
        /// Coroutine for checking the existence of an Addressable asset.
        /// </summary>
        /// <typeparam name="T">The type of the asset to check.</typeparam>
        /// <param name="addressableName">The name or key of the Addressable asset.</param>
        /// <param name="onCompleted">Callback invoked with the operation status indicating success or failure.</param>
        /// <returns>An enumerator for coroutine execution.</returns>
        private IEnumerator ExistsCoroutine<T> (string addressableName, UnityAction<AsyncOperationStatus> onCompleted)
        {
            AsyncOperationHandle<IList<IResourceLocation>> asyncOperationHandle = Addressables.LoadResourceLocationsAsync (addressableName);
            yield return asyncOperationHandle.Result;

            try
            {
                IList<IResourceLocation> resources = asyncOperationHandle.Result;

                if (resources == null || resources.Count == 0)
                {
                    Debug.LogWarning ($"[{nameof (AddressablesHelper)}] {nameof (Exists)} - Não foram encontradas ResourceLocations para o addressable '{addressableName}'.");
                    onCompleted?.Invoke (AsyncOperationStatus.Failed);
                }
                else
                {
                    bool hasType = false;

                    for (int i = 0; i < resources.Count && !hasType; i++)
                        if (resources[i].ResourceType == typeof (T))
                            hasType = true;

                    if (hasType)
                    {
                        onCompleted?.Invoke (AsyncOperationStatus.Succeeded);
                    }
                    else
                    {
                        Debug.LogWarning ($"[{nameof (AddressablesHelper)}] {nameof (Exists)} - O addressable '{addressableName}' não foi encontrado com o tipo '{nameof(T)}'.");
                        onCompleted?.Invoke (AsyncOperationStatus.Failed);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError ($"[{nameof (AddressablesHelper)}] {nameof (Exists)} - Exception: {exception}");
            }
            finally
            {
                Addressables.Release (asyncOperationHandle);
            }
        }
    }

    #endregion
}
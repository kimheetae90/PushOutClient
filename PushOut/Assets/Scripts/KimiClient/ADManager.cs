using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Advertisements;

public class ADManager : MonoBehaviour, IUnityAdsListener
{
    public static ADManager Instance;

#if UNITY_IOS
   private string gameId = "3632994";
    private bool testMode = false;
#elif UNITY_ANDROID
    private string gameId = "3632995";
    private bool testMode = false;
#else
    private const string gameId = "3632995";
    private bool testMode = true;
#endif

    public string BannerPlacementID = "banner";
    public string ScreenPlacementID = "screen";
    public string RewardPlacementID = "reward";

    public bool IsRunningRewardAD { get; private set; }

    Coroutine coroutin;

    Action OnRewardAdFinish;
    Action OnRewardAdSkip;
    Action OnRewardAdFail;

    private void Awake()
    {
        Instance = this;
        Advertisement.AddListener(this);
        Advertisement.Initialize(gameId, testMode);
    }

    public void ShowBanner()
    {
        coroutin = StartCoroutine(ShowBannerWhenReady());
    }

    public void HideBanner()
    {
        Advertisement.Banner.Hide(false);
    }

    IEnumerator ShowBannerWhenReady()
    {
        while (!Advertisement.IsReady(BannerPlacementID))
        {
            yield return new WaitForSeconds(0.1f);
        }
        Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
        Advertisement.Banner.Show(BannerPlacementID);
    }


    public void ShowScreen()
    {
        Advertisement.Show(ScreenPlacementID);
    }

    public void ShowReward(Action onFinish, Action onSkip, Action onFail)
    {
        OnRewardAdFinish = onFinish;
        OnRewardAdSkip = onSkip;
        OnRewardAdFail = onFail;
        IsRunningRewardAD = true;
        Advertisement.Show(RewardPlacementID);
    }

    public void OnUnityAdsReady(string placementId)
    {
    }

    public void OnUnityAdsDidError(string message)
    {
        Debug.LogWarning("Video failed to show");
    }

    public void OnUnityAdsDidStart(string placementId)
    {
        IsRunningRewardAD = true;
    }

    public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
    {
        // Define conditional logic for each ad completion status:
        if (showResult == ShowResult.Finished)
        {
            if (OnRewardAdFinish != null)
            {
                OnRewardAdFinish();
                OnRewardAdFinish = null;
            }
        }
        else if (showResult == ShowResult.Skipped)
        {
            if(OnRewardAdSkip != null)
            {
                OnRewardAdSkip();
                OnRewardAdSkip = null;
            }
        }
        else if (showResult == ShowResult.Failed)
        {
            if(OnRewardAdFail != null)
            {
                OnRewardAdFail();
                OnRewardAdFail = null;
            }
        }

        IsRunningRewardAD = false;
    }
    private void OnDestroy()
    {
        HideBanner();

        if (coroutin != null)
            StopCoroutine(coroutin);

        coroutin = null;
        IsRunningRewardAD = false;
    }
}

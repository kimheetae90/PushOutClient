﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class AIController
{
    private Dictionary<string, Entity> entityDic;
    private Entity entity;
    private Entity target;

    private Vector2 mainDirection;
    private Vector2 subDirection;

    private float checkTimeDel = 0.0f;
    private float checkTime = 0.2f;

    private bool bRevive = false;

    private DummyServer server;
    private float initPosX = 0;
    private float initPosY = 0;

    public string UserID { get; private set; }

    public void CachePlayModeResource(string id, DummyServer server, float posX, float posY)
    {
        PlayMode playMode = GameClient.Instance.Game as PlayMode;

        UserID = id;

        if (playMode != null)
        {
            entityDic = playMode.EntitiesDic;
        }

        if (entityDic != null)
        {
            entityDic.TryGetValue(UserID, out entity);
        }

        this.server = server;
        initPosX = posX;
        initPosY = posY;
    }

    public void CacheTutorialModeResource(string id, DummyServer server)
    {
        TutorialMode tutorialMode = GameClient.Instance.Game as TutorialMode;

        UserID = id;

        if (tutorialMode != null)
        {
            entityDic = tutorialMode.EntitiesDic;
        }

        if (entityDic != null)
        {
            entityDic.TryGetValue(UserID, out entity);
        }

        this.server = server;
    }

    public void Compute()
    {
        checkTimeDel += Time.deltaTime;
        if (checkTimeDel < checkTime)
            return;
        
        if (entity.state == 2)
        {
            if (bRevive)
            {
                server.Retry(entity.id, initPosX, initPosY);
                bRevive = false;
            }
            else
            {
                checkTime = 1f;
                bRevive = true;
            }
            return;
        }

        checkTimeDel = 0;

        GetSubDirection();
        GetTarget();

        if (target == null)
        {
            Shot();
            return;
        }

        var now = DateTime.Now.ToLocalTime();
        var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
        long currentTime = (long)span.TotalMilliseconds;
        long currentAIPushoutTick = (entity.state == 0) ? currentTime : entity.startPushOutTick;
        float diff = (currentTime - currentAIPushoutTick) * 0.001f;

        Vector2 targetPos = new Vector2(target.positionX * 1000, target.positionY * 1000);
        Vector2 aiPos = new Vector2(entity.positionX * 1000, entity.positionY * 1000);
        float distance = Vector2.Distance(targetPos, aiPos) * 0.001f;
        if (distance > PushOutForce.MAX_PUSHOUT_FORCE * 1.2 && distance < PushOutForce.MAX_PUSHOUT_FORCE * 1.5)
        {
            mainDirection = targetPos - aiPos;
            Move();
        }
        else if (distance > PushOutForce.MAX_PUSHOUT_FORCE)
        {
            if (diff > PushOutForce.MAX_PUSHOUT_FORCE * 0.5f)
            {
                mainDirection = targetPos - aiPos;
                Move();
            }
            else
            {
                float rand = UnityEngine.Random.Range(0.1f, 0.3f);
                if (rand > 0.2f)
                {
                    mainDirection = targetPos - aiPos;
                    Move();
                    checkTime = rand;
                }
                else
                {
                    mainDirection = aiPos - targetPos;
                    Move();
                }
            }
        }
        else if (distance > PushOutForce.MAX_PUSHOUT_FORCE * 0.5f)
        {
            if (diff > PushOutForce.MAX_PUSHOUT_FORCE * 0.5f)
            {
                float rand = UnityEngine.Random.Range(0.1f, 0.3f);
                if(rand > 0.2f)
                {
                    Shot();   
                }
                else
                {
                    mainDirection = targetPos - aiPos;
                    Move();
                }
            }
            else
            {
                mainDirection = aiPos - targetPos;
                Move();
            }
        }
        else if (distance > PushOutForce.MAX_PUSHOUT_FORCE * 0.15f)
        {
            if (diff > PushOutForce.MAX_PUSHOUT_FORCE * 0.8f)
            {
                Shot();
            }
            else if(diff > PushOutForce.MAX_PUSHOUT_FORCE * 0.4f)
            {
                float rand = UnityEngine.Random.Range(0.1f, 0.8f);
                if(rand > 0.4f)
                {
                    Shot();
                }
                else
                {
                    mainDirection = aiPos - targetPos;
                    Move();
                }
            }
            else
            {
                mainDirection = aiPos - targetPos;
                Move();
            }
        }
    }

    private void GetTarget()
    {
        float minTargetDistance = float.MaxValue;
        Entity minTargetEntity = null;

        Vector2 aiPos = new Vector2(entity.positionX, entity.positionY);
        foreach (var node in entityDic)
        {
            Entity iterEntity = node.Value;
            if (iterEntity.id == UserID)
                continue;

            if (iterEntity.state == 2)
                continue;

            Vector2 iterEntityPos = new Vector2(iterEntity.positionX, iterEntity.positionY);
            float iterDistance = Vector2.Distance(iterEntityPos, aiPos);
            if (iterDistance < minTargetDistance)
            {
                minTargetDistance = iterDistance;
                minTargetEntity = iterEntity;
            }
        }

        target = minTargetEntity;
    }


    private void GetSubDirection()
    {
        Vector2 aiPos = new Vector2(entity.positionX * 1000, entity.positionY * 1000);
        subDirection = aiPos * (aiPos.magnitude - 1300) * -0.005f;
    }

    private void Move()
    {
        Vector2 finalDir = mainDirection + subDirection;

        float total = Math.Abs(finalDir.x) + Math.Abs(finalDir.y);
        if (total == 0) total = 1;

        float dirX = finalDir.x / total * 1000;
        float dirY = finalDir.y / total * 1000;

        server.Move(entity.id, dirX, dirY);

    }

    private void Shot()
    {
        server.Move(entity.id, 0, 0);
    }
}
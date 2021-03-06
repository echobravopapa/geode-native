/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

#pragma warning disable 618

namespace Apache.Geode.Client.UnitTests
{
  using NUnit.Framework;
  using Apache.Geode.DUnitFramework;
  using Apache.Geode.Client.Tests;
  using Apache.Geode.Client;
  using DeltaEx = Apache.Geode.Client.Tests.DeltaEx;

  namespace failing
  {
    public class CqDeltaListener<TKey, TResult> : ICqListener<TKey, TResult>
    {

      public CqDeltaListener()
      {
        m_deltaCount = 0;
        m_valueCount = 0;
      }

      public void OnEvent(CqEvent<TKey, TResult> aCqEvent)
      {
        byte[] deltaValue = aCqEvent.getDeltaValue();
        DeltaTestImpl newValue = new DeltaTestImpl();
        DataInput input = CacheHelper.DCache.CreateDataInput(deltaValue);
        newValue.FromDelta(input);
        if (newValue.GetIntVar() == 5)
        {
          m_deltaCount++;
        }
        DeltaTestImpl fullObject = (DeltaTestImpl)(object)aCqEvent.getNewValue();
        if (fullObject.GetIntVar() == 5)
        {
          m_valueCount++;
        }

      }

      public void OnError(CqEvent<TKey, TResult> aCqEvent)
      {
      }

      public void Close()
      {
      }

      public int GetDeltaCount()
      {
        return m_deltaCount;
      }

      public int GetValueCount()
      {
        return m_valueCount;
      }

      private int m_deltaCount;
      private int m_valueCount;
    }

    public class DeltaTestAD : IDelta, IDataSerializable
    {
      private int _deltaUpdate;
      private string _staticData;

      public static DeltaTestAD Create()
      {
        return new DeltaTestAD();
      }

      public DeltaTestAD()
      {
        _deltaUpdate = 1;
        _staticData = "Data which don't get updated";
      }


      #region IDelta Members

      public void FromDelta(DataInput input)
      {
        _deltaUpdate = input.ReadInt32();
      }

      public bool HasDelta()
      {
        _deltaUpdate++;
        bool isDelta = (_deltaUpdate % 2) == 1;
        Util.Log("In DeltaTestAD.HasDelta _deltaUpdate:" + _deltaUpdate + " : isDelta:" + isDelta);
        return isDelta;
      }

      public void ToDelta(DataOutput output)
      {
        output.WriteInt32(_deltaUpdate);
      }

      #endregion

      #region IDataSerializable Members

      public int ClassId
      {
        get { return 151; }
      }

      public void FromData(DataInput input)
      {
        _deltaUpdate = input.ReadInt32();
        _staticData = input.ReadUTF();
      }

      public UInt64 ObjectSize
      {
        get { return (uint)(4 + _staticData.Length); }
      }

      public void ToData(DataOutput output)
      {
        output.WriteInt32(_deltaUpdate);
        output.WriteUTF(_staticData);
      }

      public int DeltaUpdate
      {
        get { return _deltaUpdate; }
        set { _deltaUpdate = value; }
      }

      #endregion
    }

    [TestFixture]
    [Category("group1")]
    [Category("unicast_only")]
    [Category("generics")]
    public class ThinClientDeltaTest : ThinClientRegionSteps
    {
      #region Private members

      private UnitProcess m_client1, m_client2;
      private CqDeltaListener<object, DeltaTestImpl> myCqListener;

      #endregion

      protected override ClientBase[] GetClients()
      {
        m_client1 = new UnitProcess();
        m_client2 = new UnitProcess();
        return new ClientBase[] { m_client1, m_client2 };
      }

      [TestFixtureTearDown]
      public override void EndTests()
      {
        CacheHelper.StopJavaServers();
        base.EndTests();
      }

      [TearDown]
      public override void EndTest()
      {
        try
        {
          CacheHelper.ClearEndpoints();
          CacheHelper.ClearLocators();
        }
        finally
        {
          CacheHelper.StopJavaServers();
          CacheHelper.StopJavaLocators();
        }
        base.EndTest();
      }

      public void createLRURegionAndAttachPool(string regionName, string poolName)
      {
        CacheHelper.CreateLRUTCRegion_Pool<object, object>(regionName, true, true, null, null, poolName, false, 3);
      }

      public void createRegionAndAttachPool(string regionName, string poolName, bool cloningEnabled)
      {
        CacheHelper.CreateTCRegion_Pool<object, object>(regionName, true, true, null, null, poolName, false,
          false, cloningEnabled);
      }

      public void createPool(string name, string locators, string serverGroup,
        int redundancy, bool subscription)
      {
        CacheHelper.CreatePool<object, object>(name, locators, serverGroup, redundancy, subscription);
      }

      void DoNotificationWithDelta()
      {
        try
        {
          CacheHelper.DCache.TypeRegistry.RegisterType(DeltaEx.create);
        }
        catch (IllegalStateException)
        {
          //do nothig.
        }

        string cKey = m_keys[0];
        DeltaEx val = new DeltaEx();
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        reg[cKey] = val;
        val.SetDelta(true);
        reg[cKey] = val;

        string cKey1 = m_keys[1];
        DeltaEx val1 = new DeltaEx();
        reg[cKey1] = val1;
        val1.SetDelta(true);
        reg[cKey1] = val1;
        DeltaEx.ToDeltaCount = 0;
        DeltaEx.ToDataCount = 0;
      }

      void DoNotificationWithDefaultCloning()
      {
        string cKey = m_keys[0];
        DeltaTestImpl val = new DeltaTestImpl();
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        reg[cKey] = val;
        val.SetIntVar(2);
        val.SetDelta(true);
        reg[cKey] = val;

        javaobject.PdxDelta pd = new javaobject.PdxDelta(1001);
        for (int i = 0; i < 10; i++)
        {
          reg["pdxdelta"] = pd;
        }
      }

      void DoNotificationWithDeltaLRU()
      {
        try
        {
          CacheHelper.DCache.TypeRegistry.RegisterType(DeltaEx.create);
        }
        catch (IllegalStateException)
        {
          //do nothig.
        }

        string cKey1 = "key1";
        string cKey2 = "key2";
        string cKey3 = "key3";
        string cKey4 = "key4";
        string cKey5 = "key5";
        string cKey6 = "key6";
        DeltaEx val1 = new DeltaEx();
        DeltaEx val2 = new DeltaEx();
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        reg[cKey1] = val1;
        reg[cKey2] = val1;
        reg[cKey3] = val1;
        reg[cKey4] = val1;
        reg[cKey5] = val1;
        reg[cKey6] = val1;
        val2.SetDelta(true);
        reg[cKey1] = val2;

        DeltaEx.ToDeltaCount = 0;
        DeltaEx.ToDataCount = 0;
      }

      void DoExpirationWithDelta()
      {
        try
        {
          CacheHelper.DCache.TypeRegistry.RegisterType(DeltaEx.create);
        }
        catch (IllegalStateException)
        {
          //do nothig.
        }

        DeltaEx val1 = new DeltaEx();
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        reg[1] = val1;
        // Sleep 10 seconds to allow expiration of entry in client 2
        Thread.Sleep(10000);
        val1.SetDelta(true);
        reg[1] = val1;
        DeltaEx.ToDeltaCount = 0;
        DeltaEx.ToDataCount = 0;
      }

      void DoCqWithDelta()
      {
        string cKey1 = "key1";
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        DeltaTestImpl value = new DeltaTestImpl();
        reg[cKey1] = value;
        value.SetIntVar(5);
        value.SetDelta(true);
        reg[cKey1] = value;
      }

      void initializeDeltaClientAD()
      {
        try
        {
          CacheHelper.DCache.TypeRegistry.RegisterType(DeltaTestAD.Create);
        }
        catch (IllegalStateException)
        {
          //do nothng
        }
      }

      void DoDeltaAD_C1_1()
      {
        DeltaTestAD val = new DeltaTestAD();
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        reg.GetSubscriptionService().RegisterAllKeys();
        Util.Log("clientAD1 put");
        reg[1] = val;
        Util.Log("clientAD1 put done");
      }

      void DoDeltaAD_C2_1()
      {
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");

        Util.Log("clientAD2 get");
        DeltaTestAD val = (DeltaTestAD)reg[1];

        Assert.AreEqual(2, val.DeltaUpdate);
        Util.Log("clientAD2 get done");
        reg[1] = val;
        Util.Log("clientAD2 put done");

        javaobject.PdxDelta pd = new javaobject.PdxDelta(1001);
        for (int i = 0; i < 10; i++)
        {
          reg["pdxdelta"] = pd;
        }
      }

      void DoDeltaAD_C1_afterC2Put()
      {
        Thread.Sleep(15000);
        DeltaTestAD val = null;
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        Util.Log("client fetching entry from local cache");
        val = (DeltaTestAD)reg.GetEntry(1).Value;
        Assert.IsNotNull(val);
        Assert.AreEqual(3, val.DeltaUpdate);
        Util.Log("done");

        System.Threading.Thread.Sleep(5000);
        //Assert.Greater(javaobject.PdxDelta.GotDelta, 7, "this should have recieve delta");
        javaobject.PdxDelta pd = (javaobject.PdxDelta)(reg.GetLocalView()["pdxdelta"]);
        Assert.Greater(pd.Delta, 7, "this should have recieve delta");
      }

      void runDeltaWithAppdomian(bool cloningenable)
      {
        CacheHelper.SetupJavaServers(true, "cacheserver_with_deltaAD.xml");
        CacheHelper.StartJavaLocator(1, "GFELOC1");
        CacheHelper.StartJavaServerWithLocators(1, "GFECS5", 1);
        string regionName = "DistRegionAck";
        // if (usePools)
        {
          //CacheHelper.CreateTCRegion_Pool_AD("DistRegionAck", false, false, null, null, CacheHelper.Locators, "__TEST_POOL1__", false, false, false);
          m_client1.Call(CacheHelper.CreateTCRegion_Pool_AD1, regionName, false, true, CacheHelper.Locators, (string)"__TEST_POOL1__", true, cloningenable);
          m_client2.Call(CacheHelper.CreateTCRegion_Pool_AD1, regionName, false, true, CacheHelper.Locators, (string)"__TEST_POOL1__", false, cloningenable);

          // m_client1.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, false);
          // m_client1.Call(createRegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__");
        }


        m_client1.Call(initializeDeltaClientAD);
        m_client2.Call(initializeDeltaClientAD);

        m_client1.Call(DoDeltaAD_C1_1);
        m_client2.Call(DoDeltaAD_C2_1);
        m_client1.Call(DoDeltaAD_C1_afterC2Put);
        m_client1.Call(Close);
        m_client2.Call(Close);

        CacheHelper.StopJavaServer(1);
        CacheHelper.StopJavaLocator(1);
        CacheHelper.ClearEndpoints();
        CacheHelper.ClearLocators();
      }

      void registerClassCl2()
      {
        try
        {
          CacheHelper.DCache.TypeRegistry.RegisterType(DeltaEx.create);
        }
        catch (IllegalStateException)
        {
          //do nothing
        }
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");

        reg.GetSubscriptionService().RegisterRegex(".*");
        AttributesMutator<object, object> attrMutator = reg.AttributesMutator;
        attrMutator.SetCacheListener(new SimpleCacheListener<object, object>());
      }

      void registerClassDeltaTestImpl()
      {
        try
        {
          CacheHelper.DCache.TypeRegistry.RegisterType(DeltaTestImpl.CreateDeserializable);
        }
        catch (IllegalStateException)
        {
          // ARB: ignore exception caused by type reregistration.
        }
        DeltaTestImpl.ResetDataCount();

        Thread.Sleep(2000);
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        try
        {
          reg.GetSubscriptionService().RegisterRegex(".*");
        }
        catch (Exception)
        {
          // ARB: ignore regex exception for missing notification channel.
        }
      }

      void registerCq()
      {
        Pool thePool = CacheHelper.DCache.GetPoolManager().Find("__TEST_POOL1__");
        QueryService cqService = null;
        cqService = thePool.GetQueryService();
        CqAttributesFactory<object, DeltaTestImpl> attrFac = new CqAttributesFactory<object, DeltaTestImpl>();
        myCqListener = new CqDeltaListener<object, DeltaTestImpl>();
        attrFac.AddCqListener(myCqListener);
        CqAttributes<object, DeltaTestImpl> cqAttr = attrFac.Create();
        CqQuery<object, DeltaTestImpl> theQuery = cqService.NewCq("select * from /DistRegionAck d where d.intVar > 4", cqAttr, false);
        theQuery.Execute();
      }

      void VerifyDeltaCount()
      {
        Thread.Sleep(1000);
        Util.Log("Total Data count" + DeltaEx.FromDataCount);
        Util.Log("Total Data count" + DeltaEx.FromDeltaCount);
        if (DeltaEx.FromDataCount != 3)
          Assert.Fail("Count of fromData called should be 3 ");
        if (DeltaEx.FromDeltaCount != 2)
          Assert.Fail("Count of fromDelta called should be 2 ");
        if (SimpleCacheListener<object, object>.isSuccess == false)
          Assert.Fail("Listener failure");
        SimpleCacheListener<object, object>.isSuccess = false;
        if (DeltaEx.CloneCount != 2)
          Assert.Fail("Clone count should be 2, is " + DeltaEx.CloneCount);

        DeltaEx.FromDataCount = 0;
        DeltaEx.FromDeltaCount = 0;
        DeltaEx.CloneCount = 0;
      }

      void VerifyCloning()
      {
        Thread.Sleep(1000);
        string cKey = m_keys[0];
        IRegion<object, object> reg = CacheHelper.GetRegion<object, object>("DistRegionAck");
        DeltaTestImpl val = reg[cKey] as DeltaTestImpl;

        if (val.GetIntVar() != 2)
          Assert.Fail("Int value after cloning should be 2, is " + val.GetIntVar());
        if (DeltaTestImpl.GetFromDataCount() != 2)
          Assert.Fail("After cloning, fromDataCount should have been 2, is " + DeltaTestImpl.GetFromDataCount());
        if (DeltaTestImpl.GetToDataCount() != 1)
          Assert.Fail("After cloning, toDataCount should have been 1, is " + DeltaTestImpl.GetToDataCount());

        System.Threading.Thread.Sleep(5000);
        //Assert.Greater(javaobject.PdxDelta.GotDelta, 7, "this should have recieve delta");
        javaobject.PdxDelta pd = (javaobject.PdxDelta)(reg.GetLocalView()["pdxdelta"]);
        Assert.Greater(pd.Delta, 7, "this should have recieve delta");
      }

      void VerifyDeltaCountLRU()
      {
        Thread.Sleep(1000);
        if (DeltaEx.FromDataCount != 8)
        {
          Util.Log("DeltaEx.FromDataCount = " + DeltaEx.FromDataCount);
          Util.Log("DeltaEx.FromDeltaCount = " + DeltaEx.FromDeltaCount);
          Assert.Fail("Count should have been 8. 6 for common put and two when pulled from database and deserialized");
        }
        if (DeltaEx.FromDeltaCount != 1)
        {
          Util.Log("DeltaEx.FromDeltaCount = " + DeltaEx.FromDeltaCount);
          Assert.Fail("Count should have been 1");
        }
        DeltaEx.FromDataCount = 0;
        DeltaEx.FromDeltaCount = 0;
      }

      void VerifyCqDeltaCount()
      {
        // Wait for Cq event processing in listener
        Thread.Sleep(1000);
        if (myCqListener.GetDeltaCount() != 1)
        {
          Assert.Fail("Delta from CQ event does not have expected value");
        }
        if (myCqListener.GetValueCount() != 1)
        {
          Assert.Fail("Value from CQ event is incorrect");
        }
      }
      void VerifyExpirationDeltaCount()
      {
        Thread.Sleep(1000);
        if (DeltaEx.FromDataCount != 2)
          Assert.Fail("Count should have been 2.");
        if (DeltaEx.FromDeltaCount != 0)
          Assert.Fail("Count should have been 0.");
        DeltaEx.FromDataCount = 0;
        DeltaEx.FromDeltaCount = 0;
      }

      void runNotificationWithDelta()
      {
        CacheHelper.SetupJavaServers(true, "cacheserver_with_delta.xml");
        CacheHelper.StartJavaLocator(1, "GFELOC1");
        CacheHelper.StartJavaServerWithLocators(1, "GFECS5", 1);

        m_client1.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, true);
        m_client1.Call(createRegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__", true);

        m_client2.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, true);
        m_client2.Call(createRegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__", true);

        m_client2.Call(registerClassCl2);

        m_client1.Call(DoNotificationWithDelta);
        m_client2.Call(VerifyDeltaCount);
        m_client1.Call(Close);
        m_client2.Call(Close);

        CacheHelper.StopJavaServer(1);
        CacheHelper.StopJavaLocator(1);
        CacheHelper.ClearEndpoints();
        CacheHelper.ClearLocators();
      }

      void runNotificationWithDefaultCloning()
      {
        CacheHelper.SetupJavaServers(true, "cacheserver_with_delta_test_impl.xml");
        CacheHelper.StartJavaLocator(1, "GFELOC1");
        CacheHelper.StartJavaServerWithLocators(1, "GFECS5", 1);

        m_client1.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, true);
        m_client1.Call(createRegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__", true);

        m_client2.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, true);
        m_client2.Call(createRegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__", true);

        m_client1.Call(registerClassDeltaTestImpl);
        m_client2.Call(registerClassDeltaTestImpl);

        m_client1.Call(DoNotificationWithDefaultCloning);
        m_client2.Call(VerifyCloning);
        m_client1.Call(Close);
        m_client2.Call(Close);

        CacheHelper.StopJavaServer(1);
        CacheHelper.StopJavaLocator(1);
        CacheHelper.ClearEndpoints();
        CacheHelper.ClearLocators();
      }

      void runNotificationWithDeltaWithOverFlow()
      {
        CacheHelper.SetupJavaServers(true, "cacheserver_with_delta.xml");
        CacheHelper.StartJavaLocator(1, "GFELOC1");
        CacheHelper.StartJavaServerWithLocators(1, "GFECS1", 1);

        m_client1.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, true);
        m_client1.Call(createLRURegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__");

        m_client2.Call(createPool, "__TEST_POOL1__", CacheHelper.Locators, (string)null, 0, true);
        m_client2.Call(createLRURegionAndAttachPool, "DistRegionAck", "__TEST_POOL1__");

        m_client2.Call(registerClassCl2);

        m_client1.Call(DoNotificationWithDeltaLRU);
        m_client2.Call(VerifyDeltaCountLRU);
        m_client1.Call(Close);
        m_client2.Call(Close);
        CacheHelper.StopJavaServer(1);
        CacheHelper.StopJavaLocator(1);
        CacheHelper.ClearEndpoints();
        CacheHelper.ClearLocators();
      }

      //#region Tests
      
      [Test]
      public void PutWithDeltaADWithCloning()
      {
        runDeltaWithAppdomian(true);
      }

      [Test]
      public void NotificationWithDelta()
      {
        runNotificationWithDelta();
      }

      [Test]
      public void NotificationWithDefaultCloning()
      {
        runNotificationWithDefaultCloning();
      }

      [Test]
      public void NotificationWithDeltaWithOverFlow()
      {
        runNotificationWithDeltaWithOverFlow();
      }

      //#endregion
    }
  }
}


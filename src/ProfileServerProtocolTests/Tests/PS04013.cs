﻿using IopCommon;
using Google.Protobuf;
using IopCrypto;
using IopProtocol;
using Iop.Profileserver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ProfileServerProtocolTests.Tests
{
  /// <summary>
  /// PS04013 - Application Service Add, Remove, Query
  /// https://github.com/Internet-of-People/message-protocol/blob/master/tests/PS04.md#ps04013---application-service-add-remove-query
  /// </summary>
  public class PS04013 : ProtocolTest
  {
    public const string TestName = "PS04013";
    private static Logger log = new Logger("ProfileServerProtocolTests.Tests." + TestName);

    public override string Name { get { return TestName; } }

    /// <summary>List of test's arguments according to the specification.</summary>
    private List<ProtocolTestArgument> argumentDescriptions = new List<ProtocolTestArgument>()
    {
      new ProtocolTestArgument("Server IP", ProtocolTestArgumentType.IpAddress),
      new ProtocolTestArgument("clNonCustomer Port", ProtocolTestArgumentType.Port),
      new ProtocolTestArgument("clCustomer Port", ProtocolTestArgumentType.Port),
    };

    public override List<ProtocolTestArgument> ArgumentDescriptions { get { return argumentDescriptions; } }


    /// <summary>
    /// Implementation of the test itself.
    /// </summary>
    /// <returns>true if the test passes, false otherwise.</returns>
    public override async Task<bool> RunAsync()
    {
      IPAddress ServerIp = (IPAddress)ArgumentValues["Server IP"];
      int ClNonCustomerPort = (int)ArgumentValues["clNonCustomer Port"];
      int ClCustomerPort = (int)ArgumentValues["clCustomer Port"];
      log.Trace("(ServerIp:'{0}',ClNonCustomerPort:{1},ClCustomerPort:{2})", ServerIp, ClNonCustomerPort, ClCustomerPort);

      bool res = false;
      Passed = false;

      ProtocolClient client = new ProtocolClient();
      try
      {
        PsMessageBuilder mb = client.MessageBuilder;
        byte[] testPubKey = client.GetIdentityKeys().PublicKey;
        byte[] testIdentityId = client.GetIdentityId();

        // Step 1
        await client.ConnectAsync(ServerIp, ClNonCustomerPort, true);
        bool establishHostingOk = await client.EstablishHostingAsync();

        // Step 1 Acceptance
        bool step1Ok = establishHostingOk;
        client.CloseConnection();


        // Step 2
        await client.ConnectAsync(ServerIp, ClCustomerPort, true);
        bool checkInOk = await client.CheckInAsync();


        PsProtocolMessage requestMessage = mb.CreateUpdateProfileRequest(SemVer.V100, "Test Identity", null, new GpsLocation(0, 0), null);
        await client.SendMessageAsync(requestMessage);
        PsProtocolMessage responseMessage = await client.ReceiveMessageAsync();

        bool idOk = responseMessage.Id == requestMessage.Id;
        bool statusOk = responseMessage.Response.Status == Status.Ok;

        bool updateProfileOk = idOk && statusOk;



        List<string> asList = new List<string>() { "a", "b", "c", "d", "a" };
        requestMessage = mb.CreateApplicationServiceAddRequest(asList);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;

        bool appServiceAddOk1 = idOk && statusOk;


        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        bool isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        bool isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        byte[] receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        bool pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        SemVer receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        bool versionOk = receivedVersion.Equals(SemVer.V100);

        HashSet<string> expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "a", "b", "c", "d" };
        HashSet<string> receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        bool appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk1 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;



        asList = new List<string>() { "c","d","a","e" };
        requestMessage = mb.CreateApplicationServiceAddRequest(asList);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;

        bool appServiceAddOk2 = idOk && statusOk;


        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        versionOk = receivedVersion.Equals(SemVer.V100);

        expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "a", "b", "c", "d", "e" };
        receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk2 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;



        requestMessage = mb.CreateApplicationServiceRemoveRequest("a");
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;

        bool appServiceRemoveOk3 = idOk && statusOk;


        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        versionOk = receivedVersion.Equals(SemVer.V100);

        expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "b", "c", "d", "e" };
        receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk3 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;




        requestMessage = mb.CreateApplicationServiceRemoveRequest("a");
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.ErrorNotFound;

        bool appServiceRemoveOk4 = idOk && statusOk;


        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        versionOk = receivedVersion.Equals(SemVer.V100);

        expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "b", "c", "d", "e" };
        receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk4 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;




        asList = new List<string>() { "d", "1234567890-1234567890-1234567890-1234567890", "a", "e" };
        requestMessage = mb.CreateApplicationServiceAddRequest(asList);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.ErrorInvalidValue;
        bool detailsOk = responseMessage.Response.Details == "serviceNames[1]";

        bool appServiceAddOk5 = idOk && statusOk && detailsOk;


        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        versionOk = receivedVersion.Equals(SemVer.V100);

        expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "b", "c", "d", "e" };
        receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk5 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;




        asList = new List<string>() { "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10" };
        requestMessage = mb.CreateApplicationServiceAddRequest(asList);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;

        bool appServiceAddOk6 = idOk && statusOk;


        asList = new List<string>() { "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "b10" };
        requestMessage = mb.CreateApplicationServiceAddRequest(asList);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;

        bool appServiceAddOk7 = idOk && statusOk;



        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        versionOk = receivedVersion.Equals(SemVer.V100);

        expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "b", "c", "d", "e", "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10", "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "b10" };
        receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk7 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;



        asList = new List<string>() { "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9", "c10", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8", "d9", "d10", "e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", "e10" };
        requestMessage = mb.CreateApplicationServiceAddRequest(asList);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.ErrorQuotaExceeded;

        bool appServiceAddOk8 = idOk && statusOk;


        requestMessage = mb.CreateGetIdentityInformationRequest(testIdentityId, false, false, true);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        isHostedOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsHosted;
        isOnlineOk = responseMessage.Response.SingleResponse.GetIdentityInformation.IsOnline;

        receivedPubKey = responseMessage.Response.SingleResponse.GetIdentityInformation.IdentityPublicKey.ToByteArray();
        pubKeyOk = StructuralComparisons.StructuralComparer.Compare(receivedPubKey, testPubKey) == 0;
        receivedVersion = new SemVer(responseMessage.Response.SingleResponse.GetIdentityInformation.Version);
        versionOk = receivedVersion.Equals(SemVer.V100);

        expectedAsList = new HashSet<string>(StringComparer.Ordinal) { "b", "c", "d", "e", "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10", "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "b10" };
        receivedAsList = new HashSet<string>(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices, StringComparer.Ordinal);
        appServicesOk = expectedAsList.SetEquals(responseMessage.Response.SingleResponse.GetIdentityInformation.ApplicationServices);


        bool getIdentityInfoOk8 = idOk && statusOk && isHostedOk && isOnlineOk && pubKeyOk && versionOk && appServicesOk;


        // Step 2 Acceptance
        bool step2Ok = checkInOk && updateProfileOk && appServiceAddOk1 && getIdentityInfoOk1 && appServiceAddOk2 && getIdentityInfoOk2 && appServiceRemoveOk3
          && getIdentityInfoOk3 && appServiceRemoveOk4 && getIdentityInfoOk4 && appServiceAddOk5 && getIdentityInfoOk5 && appServiceAddOk6 && appServiceAddOk7
          && getIdentityInfoOk7 && appServiceAddOk8 && getIdentityInfoOk8;


        Passed = step1Ok && step2Ok;

        res = true;
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }
      client.Dispose();

      log.Trace("(-):{0}", res);
      return res;
    }
  }
}

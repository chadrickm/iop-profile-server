﻿using IopCommon;
using Google.Protobuf;
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
  /// PS02023 - Profile Stats
  /// https://github.com/Internet-of-People/message-protocol/blob/master/tests/PS02.md#ps02023---profile-stats---no-profile-initialization
  /// </summary>
  public class PS02023 : ProtocolTest
  {
    public const string TestName = "PS02023";
    private static Logger log = new Logger("ProfileServerProtocolTests.Tests." + TestName);

    public override string Name { get { return TestName; } }

    /// <summary>List of test's arguments according to the specification.</summary>
    private List<ProtocolTestArgument> argumentDescriptions = new List<ProtocolTestArgument>()
    {
      new ProtocolTestArgument("Server IP", ProtocolTestArgumentType.IpAddress),
      new ProtocolTestArgument("clNonCustomer Port", ProtocolTestArgumentType.Port),
    };

    public override List<ProtocolTestArgument> ArgumentDescriptions { get { return argumentDescriptions; } }


    /// <summary>Identity types for test identities.</summary>
    public static List<string> IdentityTypes = new List<string>()
    {
      "Type A",
      "Type A",
      "Type B",
      "Type Alpha",
      "Type Beta",
      "Type B",
      "Type A B",
      "Type B",
      "Type A B",
      "Type C",
    };


    /// <summary>
    /// Implementation of the test itself.
    /// </summary>
    /// <returns>true if the test passes, false otherwise.</returns>
    public override async Task<bool> RunAsync()
    {
      IPAddress ServerIp = (IPAddress)ArgumentValues["Server IP"];
      int ClNonCustomerPort = (int)ArgumentValues["clNonCustomer Port"];
      log.Trace("(ServerIp:'{0}',ClNonCustomerPort:{1})", ServerIp, ClNonCustomerPort);

      bool res = false;
      Passed = false;

      ProtocolClient client = new ProtocolClient();
      List<ProtocolClient> testIdentities = new List<ProtocolClient>();
      for (int i = 0; i < IdentityTypes.Count; i++)
        testIdentities.Add(new ProtocolClient());
      try
      {
        PsMessageBuilder mb = client.MessageBuilder;

        // Step 1
        log.Trace("Step 1");
        bool error = false;
        for (int i = 0; i < IdentityTypes.Count - 2; i++)
        {
          ProtocolClient cl = testIdentities[i];
          await cl.ConnectAsync(ServerIp, ClNonCustomerPort, true);
          if (!await cl.EstablishHostingAsync(IdentityTypes[i]))
          {
            error = true;
            break;
          }
          cl.CloseConnection();
        }

        bool hostingOk = !error;




        await client.ConnectAsync(ServerIp, ClNonCustomerPort, true);
        PsProtocolMessage requestMessage = mb.CreateProfileStatsRequest();
        await client.SendMessageAsync(requestMessage);
        PsProtocolMessage responseMessage = await client.ReceiveMessageAsync();

        bool idOk = responseMessage.Id == requestMessage.Id;
        bool statusOk = responseMessage.Response.Status == Status.Ok;
        bool countOk = responseMessage.Response.SingleResponse.ProfileStats.Stats.Count == 0;

        // Step 1 Acceptance
        bool step1Ok = idOk && statusOk && countOk;

        log.Trace("Step 1: {0}", step1Ok ? "PASSED" : "FAILED");



        // Step 2
        log.Trace("Step 2");
        for (int i = IdentityTypes.Count - 2; i < IdentityTypes.Count; i++)
        {
          ProtocolClient cl = testIdentities[i];
          await cl.ConnectAsync(ServerIp, ClNonCustomerPort, true);
          if (!await cl.EstablishHostingAsync(IdentityTypes[i]))
          {
            error = true;
            break;
          }
          cl.CloseConnection();
        }

        hostingOk = !error;

        requestMessage = mb.CreateProfileStatsRequest();
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.Ok;
        countOk = responseMessage.Response.SingleResponse.ProfileStats.Stats.Count == 0;

        // Step 2 Acceptance
        bool step2Ok = idOk && statusOk && countOk;

        log.Trace("Step 2: {0}", step1Ok ? "PASSED" : "FAILED");


        Passed = step1Ok && step2Ok;

        res = true;
      }
      catch (Exception e)
      {
        log.Error("Exception occurred: {0}", e.ToString());
      }

      foreach (ProtocolClient cl in testIdentities)
        cl.Dispose();

      client.Dispose();

      log.Trace("(-):{0}", res);
      return res;
    }
  }
}

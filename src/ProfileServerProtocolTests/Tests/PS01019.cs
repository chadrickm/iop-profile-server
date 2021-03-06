﻿using IopCommon;
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
  /// PS01019 - Neighborhood Related Calls - Bad Role
  /// https://github.com/Internet-of-People/message-protocol/blob/master/tests/PS01.md#ps01019---neighborhood-related-calls---bad-role
  /// </summary>
  public class PS01019 : ProtocolTest
  {
    public const string TestName = "PS01019";
    private static Logger log = new Logger("ProfileServerProtocolTests.Tests." + TestName);

    public override string Name { get { return TestName; } }

    /// <summary>List of test's arguments according to the specification.</summary>
    private List<ProtocolTestArgument> argumentDescriptions = new List<ProtocolTestArgument>()
    {
      new ProtocolTestArgument("Server IP", ProtocolTestArgumentType.IpAddress),
      new ProtocolTestArgument("primary Port", ProtocolTestArgumentType.Port),
    };

    public override List<ProtocolTestArgument> ArgumentDescriptions { get { return argumentDescriptions; } }


    /// <summary>
    /// Implementation of the test itself.
    /// </summary>
    /// <returns>true if the test passes, false otherwise.</returns>
    public override async Task<bool> RunAsync()
    {
      IPAddress ServerIp = (IPAddress)ArgumentValues["Server IP"];
      int PrimaryPort = (int)ArgumentValues["primary Port"];
      log.Trace("(ServerIp:'{0}',PrimaryPort:{1})", ServerIp, PrimaryPort);

      bool res = false;
      Passed = false;

      ProtocolClient client = new ProtocolClient();
      try
      {
        PsMessageBuilder mb = client.MessageBuilder;

        // Step 1
        await client.ConnectAsync(ServerIp, PrimaryPort, false);

        PsProtocolMessage requestMessage = mb.CreateStartNeighborhoodInitializationRequest(1, 1, ServerIp);
        await client.SendMessageAsync(requestMessage);
        PsProtocolMessage responseMessage = await client.ReceiveMessageAsync();

        bool idOk = responseMessage.Id == requestMessage.Id;
        bool statusOk = responseMessage.Response.Status == Status.ErrorBadRole;
        bool startNeighborhoodInitializationOk = idOk && statusOk;


        requestMessage = mb.CreateFinishNeighborhoodInitializationRequest();
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.ErrorBadRole;
        bool finishNeighborhoodInitializationOk = idOk && statusOk;


        requestMessage = mb.CreateNeighborhoodSharedProfileUpdateRequest(null);
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.ErrorBadRole;
        bool neighborhoodSharedProfileUpdateOk = idOk && statusOk;


        requestMessage = mb.CreateStopNeighborhoodUpdatesRequest();
        await client.SendMessageAsync(requestMessage);
        responseMessage = await client.ReceiveMessageAsync();

        idOk = responseMessage.Id == requestMessage.Id;
        statusOk = responseMessage.Response.Status == Status.ErrorBadRole;
        bool stopNeighborhoodUpdatesOk = idOk && statusOk;

        // Step 1 Acceptance

        Passed = startNeighborhoodInitializationOk && finishNeighborhoodInitializationOk && neighborhoodSharedProfileUpdateOk && stopNeighborhoodUpdatesOk;

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

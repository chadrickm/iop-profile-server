﻿using IopProtocol;
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
using IopCommon;

namespace ProfileServerProtocolTests.Tests
{
  /// <summary>
  /// PS01004 - List Roles
  /// https://github.com/Internet-of-People/message-protocol/blob/master/tests/PS01.md#ps01004---list-roles
  /// </summary>
  public class PS01004 : ProtocolTest
  {
    public const string TestName = "PS01004";
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

        PsProtocolMessage requestMessage = mb.CreateListRolesRequest();
        await client.SendMessageAsync(requestMessage);

        PsProtocolMessage responseMessage = await client.ReceiveMessageAsync();

        // Step 1 Acceptance
        bool idOk = responseMessage.Id == requestMessage.Id;
        bool statusOk = responseMessage.Response.Status == Status.Ok;
        bool primaryPortOk = false;
        bool srNeighborPortOk = false;
        bool clNonCustomerPortOk = false;
        bool clCustomerPortOk = false;
        bool clAppServicePortOk = false;

        bool error = false;

        HashSet<uint> encryptedPorts = new HashSet<uint>();
        HashSet<uint> unencryptedPorts = new HashSet<uint>();

        foreach (Iop.Profileserver.ServerRole serverRole in responseMessage.Response.SingleResponse.ListRoles.Roles)
        {
          switch (serverRole.Role)
          {
            case ServerRoleType.Primary:
              unencryptedPorts.Add(serverRole.Port);
              primaryPortOk = serverRole.IsTcp && !serverRole.IsTls && !encryptedPorts.Contains(serverRole.Port);
              log.Trace("Primary port is {0}OK: TCP is {1}, TLS is {2}, Port no. is {3}, encrypted port list: {4}", primaryPortOk ? "" : "NOT ", serverRole.IsTcp, serverRole.IsTls, serverRole.Port, string.Join(",", encryptedPorts));
              break;

            case ServerRoleType.SrNeighbor:
              encryptedPorts.Add(serverRole.Port);
              srNeighborPortOk = serverRole.IsTcp && serverRole.IsTls && !unencryptedPorts.Contains(serverRole.Port);
              log.Trace("Server Neighbor port is {0}OK: TCP is {1}, TLS is {2}, Port no. is {3}, unencrypted port list: {4}", srNeighborPortOk ? "" : "NOT ", serverRole.IsTcp, serverRole.IsTls, serverRole.Port, string.Join(",", unencryptedPorts));
              break;

            
            case ServerRoleType.ClNonCustomer:
              encryptedPorts.Add(serverRole.Port);
              clNonCustomerPortOk = serverRole.IsTcp && serverRole.IsTls && !unencryptedPorts.Contains(serverRole.Port);
              log.Trace("Client Non-customer port is {0}OK: TCP is {1}, TLS is {2}, Port no. is {3}, unencrypted port list: {4}", clNonCustomerPortOk ? "" : "NOT ", serverRole.IsTcp, serverRole.IsTls, serverRole.Port, string.Join(",", unencryptedPorts));
              break;

            case ServerRoleType.ClCustomer:
              encryptedPorts.Add(serverRole.Port);
              clCustomerPortOk = serverRole.IsTcp && serverRole.IsTls && !unencryptedPorts.Contains(serverRole.Port);
              log.Trace("Client Customer port is {0}OK: TCP is {1}, TLS is {2}, Port no. is {3}, unencrypted port list: {4}", clCustomerPortOk ? "" : "NOT ", serverRole.IsTcp, serverRole.IsTls, serverRole.Port, string.Join(",", unencryptedPorts));
              break;

            case ServerRoleType.ClAppService:
              encryptedPorts.Add(serverRole.Port);
              clAppServicePortOk = serverRole.IsTcp && serverRole.IsTls && !unencryptedPorts.Contains(serverRole.Port);
              log.Trace("Client AppService port is {0}OK: TCP is {1}, TLS is {2}, Port no. is {3}, unencrypted port list: {4}", clAppServicePortOk ? "" : "NOT ", serverRole.IsTcp, serverRole.IsTls, serverRole.Port, string.Join(",", unencryptedPorts));
              break;

            default:
              log.Error("Unknown server role {0}.", serverRole.Role);
              error = true;
              break;
          }
        }

        bool portsOk = primaryPortOk
          && srNeighborPortOk
          && clNonCustomerPortOk
          && clCustomerPortOk
          && clAppServicePortOk;


        Passed = !error && idOk && statusOk && portsOk;

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

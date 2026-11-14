// ============================================================================
// File: KiranaPacketShield.cs
// Namespace: KiranaHub.PacketFilterEbpf.Filter
// Description: Programmatic compiler for KiranaHub XDP DDoS Protection eBPF bytecode
// and line-rate synthetic packet generation for high-throughput benchmarking.
// ============================================================================

using System.Buffers.Binary;
using KiranaHub.PacketFilterEbpf.Vm;

namespace KiranaHub.PacketFilterEbpf.Filter;

/// <summary>
/// Compiles Kirana quick-commerce DDoS protection firewall rules into sandboxed eBPF bytecode.
/// </summary>
public static class KiranaPacketShield
{
    // [KYA KARTA HAI]: Known malicious botnet subnet CIDR IP define karta hai (198.51.100.0/24 in Big Endian integer).
    // [KYU ZAROORI HAI]: Attacker IP filtering benchmark baseline.
    // [KAISE CHALTA HAI]: 198.51.100.0 represented as uint 0xC6336400.
    // [OUTPUT / SIDE EFFECT]: Malicious CIDR constant.
    // [VARIATIONS TO TRY]: Dynamic eBPF LPM trie map for CIDR lookups.
    public const uint BlacklistedSubnet = 0xC6336400;

    // [KYA KARTA HAI]: 24-bit subnet mask define karta hai (0xFFFFFF00).
    // [KYU ZAROORI HAI]: Masking IP addresses to match /24 CIDR block.
    // [KAISE CHALTA HAI]: Constant uint 0xFFFFFF00.
    // [OUTPUT / SIDE EFFECT]: CIDR mask.
    // [VARIATIONS TO TRY]: /16 or /32 single IP mask.
    public const uint SubnetMask24 = 0xFFFFFF00;

    // [KYA KARTA HAI]: Legitimate KiranaHub API destination port define karta hai (5000).
    // [KYU ZAROORI HAI]: Filter drops all traffic targeted at unapproved edge ports.
    // [KAISE CHALTA HAI]: Constant ushort 5000.
    // [OUTPUT / SIDE EFFECT]: Valid edge port.
    // [VARIATIONS TO TRY]: Port 443 (HTTPS) or 80 (HTTP).
    public const ushort KiranaApiPort = 5000;

    // [KYA KARTA HAI]: KiranaHub DDoS Shield eBPF bytecode program compile karta hai.
    // [KYU ZAROORI HAI]: In-kernel XDP filter bytecode generate karta hai for line-rate packet dropping.
    // [KAISE CHALTA HAI]: Builds sequence of bounds checks, protocol checks, IP blacklist checks, and port checks.
    // [OUTPUT / SIDE EFFECT]: Returns EbpfInstruction[] bytecode program.
    // [VARIATIONS TO TRY]: Rate-limiting token bucket instruction block.
    public static EbpfInstruction[] CompileDdosFilter()
    {
        // [KYA KARTA HAI]: Bytecode instruction list allocate karta hai.
        // [KYU ZAROORI HAI]: Accumulates compiled eBPF instructions.
        // [KAISE CHALTA HAI]: new List<EbpfInstruction>().
        // [OUTPUT / SIDE EFFECT]: Instruction list.
        // [VARIATIONS TO TRY]: Fixed-size array builder.
        var instructions = new List<EbpfInstruction>();

        // ---------------------------------------------------------------------
        // Step 1: Minimum Packet Length Bounds Check (offset: R1 < 38 => DROP)
        // ---------------------------------------------------------------------
        // [KYA KARTA HAI]: Packet length check karta hai (R1 >= 38 bytes).
        // [KYU ZAROORI HAI]: Prevents out-of-bounds read on truncated / fragmented packets.
        // [KAISE CHALTA HAI]: if R1 >= 38 jump +1 (to step 2); else fall through to DROP.
        // [OUTPUT / SIDE EFFECT]: Emits JumpGreaterOrEqualImm.
        // [VARIATIONS TO TRY]: Check minimum 54 bytes (full TCP payload header).
        instructions.Add(EbpfInstruction.JumpGreaterOrEqualImm(1, 38, 2)); // PC 0: If >= 38, jump to PC 3

        // PC 1: Drop Action
        instructions.Add(EbpfInstruction.MovImm(0, (int)XdpAction.Drop));   // PC 1: R0 = XDP_DROP
        instructions.Add(EbpfInstruction.Exit());                           // PC 2: EXIT (DROP)

        // ---------------------------------------------------------------------
        // Step 2: Protocol Check (offset 23 must be 6 for TCP)
        // ---------------------------------------------------------------------
        // [KYA KARTA HAI]: Load IP Protocol byte from packet offset 23 into R2.
        // [KYU ZAROORI HAI]: Verify packet is TCP (protocol 6).
        // [KAISE CHALTA HAI]: Emits LoadByte(R2, R1, 23).
        // [OUTPUT / SIDE EFFECT]: R2 loaded with protocol.
        // [VARIATIONS TO TRY]: Allow UDP (17) for QUIC/HTTP3.
        instructions.Add(EbpfInstruction.LoadByte(2, 1, 23));               // PC 3: R2 = *(uint8*)(packet + 23)

        // [KYA KARTA HAI]: Check if R2 == 6 (TCP).
        // [KYU ZAROORI HAI]: Drop rogue UDP floods or IGMP packets.
        // [KAISE CHALTA HAI]: If R2 == 6 jump +2 (to step 3); else drop.
        // [OUTPUT / SIDE EFFECT]: Emits JumpEqualImm.
        // [VARIATIONS TO TRY]: Branch to pass if DNS.
        instructions.Add(EbpfInstruction.JumpEqualImm(2, 6, 2));            // PC 4: If R2 == 6, jump to PC 7

        // PC 5: Drop Action
        instructions.Add(EbpfInstruction.MovImm(0, (int)XdpAction.Drop));   // PC 5: R0 = XDP_DROP
        instructions.Add(EbpfInstruction.Exit());                           // PC 6: EXIT (DROP)

        // ---------------------------------------------------------------------
        // Step 3: Source IP Blacklist Subnet Check (offset 26..29)
        // ---------------------------------------------------------------------
        // [KYA KARTA HAI]: Load IPv4 source address (32 bits) from packet offset 26 into R2.
        // [KYU ZAROORI HAI]: Inspect attacker source IP address.
        // [KAISE CHALTA HAI]: Emits LoadWord(R2, R1, 26).
        // [OUTPUT / SIDE EFFECT]: R2 loaded with Source IP.
        // [VARIATIONS TO TRY]: IPv6 128-bit address check.
        instructions.Add(EbpfInstruction.LoadWord(2, 1, 26));               // PC 7: R2 = *(uint32*)(packet + 26)

        // [KYA KARTA HAI]: Copy R2 into R3.
        // [KYU ZAROORI HAI]: Preserve original IP while masking subnet in R3.
        // [KAISE CHALTA HAI]: Emits MovReg(R3, R2).
        // [OUTPUT / SIDE EFFECT]: R3 = R2.
        // [VARIATIONS TO TRY]: None.
        instructions.Add(EbpfInstruction.MovReg(3, 2));                     // PC 8: R3 = R2

        // [KYA KARTA HAI]: Apply /24 subnet mask to R3 (R3 &= 0xFFFFFF00).
        // [KYU ZAROORI HAI]: Zero out host bits to isolate network prefix.
        // [KAISE CHALTA HAI]: Emits AndImm(R3, 0xFFFFFF00).
        // [OUTPUT / SIDE EFFECT]: R3 masked.
        // [VARIATIONS TO TRY]: Dynamic mask.
        instructions.Add(EbpfInstruction.AndImm(3, unchecked((int)SubnetMask24)));     // PC 9: R3 &= 0xFFFFFF00

        // [KYA KARTA HAI]: Compare R3 with BlacklistedSubnet (0xC6336400).
        // [KYU ZAROORI HAI]: If source IP matches rogue botnet subnet, drop packet immediately!
        // [KAISE CHALTA HAI]: If R3 != BlacklistedSubnet jump +2 (to step 4); else fall through to DROP.
        // [OUTPUT / SIDE EFFECT]: Emits JumpNotEqualImm.
        // [VARIATIONS TO TRY]: Range check.
        instructions.Add(EbpfInstruction.JumpNotEqualImm(3, unchecked((int)BlacklistedSubnet), 2)); // PC 10: If not rogue, jump to PC 13

        // PC 11: Drop Action (Blacklisted Botnet IP)
        instructions.Add(EbpfInstruction.MovImm(0, (int)XdpAction.Drop));   // PC 11: R0 = XDP_DROP
        instructions.Add(EbpfInstruction.Exit());                           // PC 12: EXIT (DROP)

        // ---------------------------------------------------------------------
        // Step 4: Destination Port Check (offset 36..37 must be KiranaApiPort 5000)
        // ---------------------------------------------------------------------
        // [KYA KARTA HAI]: Load TCP destination port (16 bits) from packet offset 36 into R2.
        // [KYU ZAROORI HAI]: Verify packet is destined for approved Kirana API port.
        // [KAISE CHALTA HAI]: Emits LoadHalf(R2, R1, 36).
        // [OUTPUT / SIDE EFFECT]: R2 loaded with destination port.
        // [VARIATIONS TO TRY]: Check port range 5000-5010.
        instructions.Add(EbpfInstruction.LoadHalf(2, 1, 36));               // PC 13: R2 = *(uint16*)(packet + 36)

        // [KYA KARTA HAI]: Check if R2 == KiranaApiPort (5000).
        // [KYU ZAROORI HAI]: Port scan or rogue port traffic should be dropped.
        // [KAISE CHALTA HAI]: If R2 == 5000 jump +2 (to PASS); else drop.
        // [OUTPUT / SIDE EFFECT]: Emits JumpEqualImm.
        // [VARIATIONS TO TRY]: Port whitelist bitmask.
        instructions.Add(EbpfInstruction.JumpEqualImm(2, KiranaApiPort, 2));// PC 14: If port == 5000, jump to PC 17

        // PC 15: Drop Action (Wrong Port)
        instructions.Add(EbpfInstruction.MovImm(0, (int)XdpAction.Drop));   // PC 15: R0 = XDP_DROP
        instructions.Add(EbpfInstruction.Exit());                           // PC 16: EXIT (DROP)

        // ---------------------------------------------------------------------
        // Step 5: Legitimate Traffic Accepted (PASS)
        // ---------------------------------------------------------------------
        // [KYA KARTA HAI]: Set R0 = XDP_PASS.
        // [KYU ZAROORI HAI]: Signals kernel to forward packet to Kirana application socket.
        // [KAISE CHALTA HAI]: Emits MovImm(0, XDP_PASS).
        // [OUTPUT / SIDE EFFECT]: R0 = 0.
        // [VARIATIONS TO TRY]: None.
        instructions.Add(EbpfInstruction.MovImm(0, (int)XdpAction.Pass));   // PC 17: R0 = XDP_PASS

        // [KYA KARTA HAI]: Program EXIT.
        // [KYU ZAROORI HAI]: Terminates execution and returns verdict.
        // [KAISE CHALTA HAI]: Emits Exit().
        // [OUTPUT / SIDE EFFECT]: Halts execution.
        // [VARIATIONS TO TRY]: None.
        instructions.Add(EbpfInstruction.Exit());                           // PC 18: EXIT (PASS)

        // [KYA KARTA HAI]: Compiled instruction array return karta hai.
        // [KYU ZAROORI HAI]: Ready for static verification and VM loading.
        // [KAISE CHALTA HAI]: instructions.ToArray().
        // [OUTPUT / SIDE EFFECT]: EbpfInstruction[] array.
        // [VARIATIONS TO TRY]: None.
        return instructions.ToArray();
    }

    // [KYA KARTA HAI]: Synthetic IPv4/TCP network packet synthesize karta hai.
    // [KYU ZAROORI HAI]: Realistic network testing and micro-benchmarking without physical NIC dependencies.
    // [KAISE CHALTA HAI]: Populates Ethernet, IPv4, and TCP headers into a 64-byte raw buffer.
    // [OUTPUT / SIDE EFFECT]: Returns byte[64] packet.
    // [VARIATIONS TO TRY]: Random payload sizes.
    public static byte[] CreatePacket(byte protocol, uint srcIp, ushort dstPort)
    {
        // [KYA KARTA HAI]: 64-byte packet buffer allocate karta hai (minimum standard Ethernet frame size).
        // [KYU ZAROORI HAI]: Standard network packet simulation.
        // [KAISE CHALTA HAI]: new byte[64].
        // [OUTPUT / SIDE EFFECT]: Packet buffer.
        // [VARIATIONS TO TRY]: 1500-byte MTU frame.
        byte[] packet = new byte[64];

        // [KYA KARTA HAI]: Ethernet Type IPv4 (0x0800) write karta hai offset 12..13 par.
        // [KYU ZAROORI HAI]: Ethernet frame demuxing.
        // [KAISE CHALTA HAI]: BinaryPrimitives.WriteUInt16BigEndian.
        // [OUTPUT / SIDE EFFECT]: EtherType set.
        // [VARIATIONS TO TRY]: VLAN tagged frame.
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(12, 2), 0x0800);

        // [KYA KARTA HAI]: IPv4 Version & Header Length write karta hai (0x45: IPv4, 20 bytes IHL).
        // [KYU ZAROORI HAI]: Valid IPv4 header byte.
        // [KAISE CHALTA HAI]: packet[14] = 0x45.
        // [OUTPUT / SIDE EFFECT]: IP header start.
        // [VARIATIONS TO TRY]: None.
        packet[14] = 0x45;

        // [KYA KARTA HAI]: IP Protocol field write karta hai offset 23 par.
        // [KYU ZAROORI HAI]: 6 = TCP, 17 = UDP.
        // [KAISE CHALTA HAI]: packet[23] = protocol.
        // [OUTPUT / SIDE EFFECT]: Protocol byte set.
        // [VARIATIONS TO TRY]: Custom raw IP.
        packet[23] = protocol;

        // [KYA KARTA HAI]: Source IPv4 address write karta hai offset 26..29 par.
        // [KYU ZAROORI HAI]: Sender IP address for filter inspection.
        // [KAISE CHALTA HAI]: BinaryPrimitives.WriteUInt32BigEndian(packet, srcIp).
        // [OUTPUT / SIDE EFFECT]: Source IP set.
        // [VARIATIONS TO TRY]: Random IP generator.
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(26, 4), srcIp);

        // [KYA KARTA HAI]: Destination IPv4 address write karta hai offset 30..33 (e.g. 10.0.0.1).
        // [KYU ZAROORI HAI]: KiranaHub server internal IP.
        // [KAISE CHALTA HAI]: WriteUInt32BigEndian 0x0A000001.
        // [OUTPUT / SIDE EFFECT]: Destination IP set.
        // [VARIATIONS TO TRY]: Public gateway IP.
        BinaryPrimitives.WriteUInt32BigEndian(packet.AsSpan(30, 4), 0x0A000001);

        // [KYA KARTA HAI]: TCP Source Port write karta hai offset 34..35 (e.g. 48291).
        // [KYU ZAROORI HAI]: Ephemeral client port.
        // [KAISE CHALTA HAI]: WriteUInt16BigEndian 48291.
        // [OUTPUT / SIDE EFFECT]: Source port set.
        // [VARIATIONS TO TRY]: Random ephemeral port.
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(34, 2), 48291);

        // [KYA KARTA HAI]: TCP Destination Port write karta hai offset 36..37.
        // [KYU ZAROORI HAI]: Target service port (e.g. 5000).
        // [KAISE CHALTA HAI]: BinaryPrimitives.WriteUInt16BigEndian(packet, dstPort).
        // [OUTPUT / SIDE EFFECT]: Destination port set.
        // [VARIATIONS TO TRY]: Random target port.
        BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(36, 2), dstPort);

        // [KYA KARTA HAI]: Synthetic packet byte array return karta hai.
        // [KYU ZAROORI HAI]: Input for eBPF virtual machine filtering.
        // [KAISE CHALTA HAI]: Returns packet.
        // [OUTPUT / SIDE EFFECT]: Synthesized packet ready.
        // [VARIATIONS TO TRY]: None.
        return packet;
    }
}

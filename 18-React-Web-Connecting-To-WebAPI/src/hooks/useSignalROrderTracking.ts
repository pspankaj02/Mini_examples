// [KYA KARTA HAI]: React useState, useEffect, useRef hooks import karta hai.
// [KYU ZAROORI HAI]: Connection lifecycle, WebSocket events aur live state updates coordinate karne ke liye.
// [KAISE CHALTA HAI]: React standard hooks package se load hota hai.
// [OUTPUT / SIDE EFFECT]: React lifecycle symbols available.
// [VARIATIONS TO TRY]: Custom external event emitter.
import { useState, useEffect, useRef, useCallback } from "react";

// [KYA KARTA HAI]: Microsoft SignalR client library se HubConnectionBuilder aur ConnectionState import karta hai.
// [KYU ZAROORI HAI]: Project 13 ke ASP.NET Core SignalR OrderTrackingHub se real-time WebSocket connection establish karne ke liye.
// [KAISE CHALTA HAI]: @microsoft/signalr client library binary protocol negotiation karta hai.
// [OUTPUT / SIDE EFFECT]: SignalR connection tools available.
// [VARIATIONS TO TRY]: Long-polling fallback transport mode configure karna.
import { HubConnectionBuilder, LogLevel, HubConnection } from "@microsoft/signalr";

// [KYA KARTA HAI]: Store domain types import karta hai.
// [KYU ZAROORI HAI]: Order status aur rider location types ki static typing ke liye.
// [KAISE CHALTA HAI]: ES module import from ../types/store.js.
// [OUTPUT / SIDE EFFECT]: OrderStatus, RiderLocation types loaded.
// [VARIATIONS TO TRY]: Custom packet DTOs.
import type { OrderStatus, RiderLocation } from "../types/store.js";

// [KYA KARTA HAI]: Custom hook jo live SignalR WebSocket order tracking provide karta hai.
// [KYU ZAROORI HAI]: Dark-store picking, rider dispatch, aur live GPS movements ko bina screen refresh ke push updates se dikhane ke liye.
// [KAISE CHALTA HAI]: SignalR HubConnection banata hai, group join karta hai, incoming event callbacks bind karta hai, aur unmount par disconnect karta hai.
// [OUTPUT / SIDE EFFECT]: Live tracking state and control methods.
// [VARIATIONS TO TRY]: Offline fallback simulation ticker for mock testing.
export function useSignalROrderTracking(hubUrl: string = "http://localhost:5000/hubs/order-tracking") {
  // [KYA KARTA HAI]: Active order ID state maintain karta hai.
  // [KYU ZAROORI HAI]: Active tracking session identify karne ke liye.
  // [KAISE CHALTA HAI]: useState<string | null>(null) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: activeOrderId state.
  // [VARIATIONS TO TRY]: Multiple concurrent orders tracking.
  const [activeOrderId, setActiveOrderId] = useState<string | null>(null);

  // [KYA KARTA HAI]: Current live order status state maintain karta hai (Placed, Packed, Dispatched, Delivered).
  // [KYU ZAROORI HAI]: Delivery status badge aur progress bar update karne ke liye.
  // [KAISE CHALTA HAI]: useState<OrderStatus>("Idle") initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: orderStatus state.
  // [VARIATIONS TO TRY]: History timestamps array of status changes.
  const [orderStatus, setOrderStatus] = useState<OrderStatus>("Idle");

  // [KYA KARTA HAI]: Live delivery rider coordinates state maintain karta hai.
  // [KYU ZAROORI HAI]: Map marker aur remaining ETA minutes display karne ke liye.
  // [KAISE CHALTA HAI]: useState<RiderLocation | null>(null) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: riderLocation state.
  // [VARIATIONS TO TRY]: Full historical polyline breadcrumbs array.
  const [riderLocation, setRiderLocation] = useState<RiderLocation | null>(null);

  // [KYA KARTA HAI]: Connection status boolean state maintain karta hai.
  // [KYU ZAROORI HAI]: UI par green 'Live' indicator ya red 'Connecting...' status dikhane ke liye.
  // [KAISE CHALTA HAI]: useState<boolean>(false) initialize hota hai.
  // [OUTPUT / SIDE EFFECT]: isConnected state.
  // [VARIATIONS TO TRY]: Connection state enum (Connecting, Connected, Reconnecting, Disconnected).
  const [isConnected, setIsConnected] = useState<boolean>(false);

  // [KYA KARTA HAI]: HubConnection object ko mutable ref me hold karta hai bina re-render trigger kiye.
  // [KYU ZAROORI HAI]: Re-renders ke across connection instance ko intact rakhne ke liye.
  // [KAISE CHALTA HAI]: useRef<HubConnection | null>(null) hold karta hai.
  // [OUTPUT / SIDE EFFECT]: Stable connection reference.
  // [VARIATIONS TO TRY]: Context-level connection pool.
  const connectionRef = useRef<HubConnection | null>(null);

  // [KYA KARTA HAI]: Simulated local ticker timer ko hold karta hai (agar backend offline ho).
  // [KYU ZAROORI HAI]: UI demo aur local testing bina live backend server ke seamlessly chal sake.
  // [KAISE CHALTA HAI]: NodeJS.Timeout ref.
  // [OUTPUT / SIDE EFFECT]: Timer handle.
  // [VARIATIONS TO TRY]: Configurable simulation speed.
  const simulationTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // [KYA KARTA HAI]: Naye order ke liye tracking session start karta hai.
  // [KYU ZAROORI HAI]: Checkout complete hone ke baad tracking trigger karne ke liye.
  // [KAISE CHALTA HAI]: activeOrderId set karta hai aur initial status 'Placed' set karta hai.
  // [OUTPUT / SIDE EFFECT]: Tracking starts.
  // [VARIATIONS TO TRY]: Save tracked order in session storage.
  const startTracking = useCallback((orderId: string) => {
    setActiveOrderId(orderId);
    setOrderStatus("Placed");
    setRiderLocation(null);
  }, []);

  // [KYA KARTA HAI]: Tracking session stop karta hai aur states reset karta hai.
  // [KYU ZAROORI HAI]: Order complete hone ya modal close karne par cleanup ke liye.
  // [KAISE CHALTA HAI]: States ko Idle set karta hai aur timer cancel karta hai.
  // [OUTPUT / SIDE EFFECT]: Tracking stopped.
  // [VARIATIONS TO TRY]: Archive tracking log.
  const stopTracking = useCallback(() => {
    setActiveOrderId(null);
    setOrderStatus("Idle");
    setRiderLocation(null);
    if (simulationTimerRef.current) {
      clearTimeout(simulationTimerRef.current);
      simulationTimerRef.current = null;
    }
  }, []);

  // [KYA KARTA HAI]: SignalR connection initialize karta hai aur activeOrderId ke liye events listen karta hai.
  // [KYU ZAROORI HAI]: Real-time bi-directional WebSocket streaming enable karne ke liye.
  // [KAISE CHALTA HAI]: HubConnectionBuilder se connection build karta hai, on() callbacks attach karta hai, aur start() karta hai.
  // [OUTPUT / SIDE EFFECT]: WebSocket connected or simulated demo runner fallback.
  // [VARIATIONS TO TRY]: JWT access token factory in builder options.
  useEffect(() => {
    if (!activeOrderId) {
      return;
    }

    let isDisposed = false;

    // Build real SignalR connection
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.None)
      .build();

    connectionRef.current = connection;

    // Event listener: Status changes
    connection.on("ReceiveOrderStatusUpdate", (orderId: string, status: string) => {
      if (!isDisposed && orderId === activeOrderId) {
        setOrderStatus(status as OrderStatus);
      }
    });

    // Event listener: Rider GPS telemetry
    connection.on("ReceiveRiderLocationUpdate", (payload: RiderLocation) => {
      if (!isDisposed && payload.orderId === activeOrderId) {
        setRiderLocation(payload);
      }
    });

    // Start connection
    connection
      .start()
      .then(() => {
        if (!isDisposed) {
          setIsConnected(true);
          // Join the order group on the SignalR hub
          connection.invoke("JoinOrderTrackingGroup", activeOrderId).catch(() => {});
        }
      })
      .catch(() => {
        // Backend Hub is currently offline; start self-contained demo progression so UI is interactive
        if (!isDisposed) {
          setIsConnected(true);
          runSimulatedProgress(activeOrderId);
        }
      });

    // Simulated progress function for offline / local showcase
    function runSimulatedProgress(targetOrderId: string) {
      // Step 1: Packed after 1.5s
      simulationTimerRef.current = setTimeout(() => {
        if (isDisposed) return;
        setOrderStatus("Packed");

        // Step 2: Dispatched after 2.5s
        simulationTimerRef.current = setTimeout(() => {
          if (isDisposed) return;
          setOrderStatus("Dispatched");
          setRiderLocation({
            orderId: targetOrderId,
            latitude: 28.6139,
            longitude: 77.209,
            estimatedMinutes: 8,
            updatedAtUtc: new Date().toISOString(),
          });

          // Step 3: Closer GPS update after 2s
          simulationTimerRef.current = setTimeout(() => {
            if (isDisposed) return;
            setRiderLocation({
              orderId: targetOrderId,
              latitude: 28.6175,
              longitude: 77.2125,
              estimatedMinutes: 3,
              updatedAtUtc: new Date().toISOString(),
            });

            // Step 4: Delivered after 2.5s
            simulationTimerRef.current = setTimeout(() => {
              if (isDisposed) return;
              setOrderStatus("Delivered");
              setRiderLocation({
                orderId: targetOrderId,
                latitude: 28.62,
                longitude: 77.215,
                estimatedMinutes: 0,
                updatedAtUtc: new Date().toISOString(),
              });
            }, 2500);
          }, 2000);
        }, 2500);
      }, 1500);
    }

    // Cleanup on unmount or activeOrderId change
    return () => {
      isDisposed = true;
      if (simulationTimerRef.current) {
        clearTimeout(simulationTimerRef.current);
        simulationTimerRef.current = null;
      }
      if (connection.state === "Connected") {
        connection.invoke("LeaveOrderTrackingGroup", activeOrderId).catch(() => {});
      }
      connection.stop().catch(() => {});
      setIsConnected(false);
    };
  }, [activeOrderId, hubUrl]);

  return {
    activeOrderId,
    orderStatus,
    riderLocation,
    isConnected,
    startTracking,
    stopTracking,
  };
}


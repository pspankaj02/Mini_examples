"use client";

// [KYA KARTA HAI]: Next.js "use client" directive define karta hai.
// [KYU ZAROORI HAI]: Browser WebGL canvas API aur interactive 3D rendering ko client side par enable karne ke liye.
// [KAISE CHALTA HAI]: Next.js is component ko client bundle me include karta hai.
// [OUTPUT / SIDE EFFECT]: Client Component boundary.
// [VARIATIONS TO TRY]: Dynamic import with ssr: false.

// [KYA KARTA HAI]: React core hooks import karta hai.
// [KYU ZAROORI HAI]: Canvas DOM reference, component lifecycle aur animation loop manage karne ke liye.
// [KAISE CHALTA HAI]: useRef aur useEffect React se load hote hain.
// [OUTPUT / SIDE EFFECT]: React hooks symbols.
// [VARIATIONS TO TRY]: useLayoutEffect for synchronous canvas measuring.
import React, { useEffect, useRef } from "react";

// [KYA KARTA HAI]: Three.js library import karta hai.
// [KYU ZAROORI HAI]: 3D WebGL rendering, geometry, materials, lighting aur camera control karne ke liye.
// [KAISE CHALTA HAI]: 'three' package se modular symbols import karta hai.
// [OUTPUT / SIDE EFFECT]: Three.js engine available.
// [VARIATIONS TO TRY]: @react-three/fiber declarative JSX wrapper.
import * as THREE from "three";

// [KYA KARTA HAI]: Procedural canvas texture generate karta hai delivery crate packaging look ke liye.
// [KYU ZAROORI HAI]: External image asset load kiye bina high-res "⚡ KiranaHub Instant Express" label aur cardboard grain texture produce karne ke liye.
// [KAISE CHALTA HAI]: HTML5 2D Canvas par cardboard color aur text render karke THREE.CanvasTexture return karta hai.
// [OUTPUT / SIDE EFFECT]: THREE.CanvasTexture instance.
// [VARIATIONS TO TRY]: Normal maps and roughness map textures.
function createProceduralCrateTexture(): THREE.CanvasTexture {
  const canvas = document.createElement("canvas");
  canvas.width = 512;
  canvas.height = 512;
  const ctx = canvas.getContext("2d");

  if (ctx) {
    // Cardboard craft base
    ctx.fillStyle = "#d97706";
    ctx.fillRect(0, 0, 512, 512);

    // Darker cardboard border frame
    ctx.lineWidth = 16;
    ctx.strokeStyle = "#92400e";
    ctx.strokeRect(8, 8, 496, 496);

    // Cross brace straps
    ctx.beginPath();
    ctx.moveTo(8, 8);
    ctx.lineTo(504, 504);
    ctx.moveTo(504, 8);
    ctx.lineTo(8, 504);
    ctx.strokeStyle = "#b45309";
    ctx.lineWidth = 10;
    ctx.stroke();

    // Central Brand Badge
    ctx.fillStyle = "#059669";
    ctx.beginPath();
    ctx.roundRect(106, 176, 300, 160, 24);
    ctx.fill();

    // Text Label: KiranaHub
    ctx.fillStyle = "#ffffff";
    ctx.font = "bold 38px sans-serif";
    ctx.textAlign = "center";
    ctx.fillText("🛒 KiranaHub", 256, 240);

    // Tagline: 10 MINS DELIVERY
    ctx.fillStyle = "#fef3c7";
    ctx.font = "bold 20px sans-serif";
    ctx.fillText("⚡ 10 MINS EXPRESS", 256, 280);
  }

  const texture = new THREE.CanvasTexture(canvas);
  return texture;
}

// [KYA KARTA HAI]: Three.js 3D Delivery Crate visualizer component render karta hai.
// [KYU ZAROORI HAI]: Modern 3D graphics experience show karne ke liye (interactive 360 rotation of the grocery box).
// [KAISE CHALTA HAI]: Canvas container me WebGL scene build karta hai, continuous animation loop run karta hai, aur drag/touch rotation support karta hai.
// [OUTPUT / SIDE EFFECT]: Interactive 3D WebGL Canvas.
// [VARIATIONS TO TRY]: Add fruit/vegetable 3D meshes popping out of the crate.
export const ThreeDeliveryCrate: React.FC = () => {
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    // 1. Scene setup
    const scene = new THREE.Scene();
    scene.background = new THREE.Color("#f0fdf4"); // subtle mint background

    // 2. Camera setup
    const width = container.clientWidth || 360;
    const height = 300;
    const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);
    camera.position.set(0, 1.2, 3.8);
    camera.lookAt(0, 0, 0);

    // 3. WebGL Renderer setup
    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    container.appendChild(renderer.domElement);

    // 4. Lighting setup
    const ambientLight = new THREE.AmbientLight(0xffffff, 1.2);
    scene.add(ambientLight);

    const dirLight = new THREE.DirectionalLight(0xffffff, 2.0);
    dirLight.position.set(4, 6, 4);
    dirLight.castShadow = true;
    scene.add(dirLight);

    const pointLight = new THREE.PointLight(0x10b981, 1.5, 10);
    pointLight.position.set(-2, 2, 2);
    scene.add(pointLight);

    // 5. Procedural 3D Crate Mesh
    const texture = createProceduralCrateTexture();
    const geometry = new THREE.BoxGeometry(1.6, 1.4, 1.6);
    const material = new THREE.MeshStandardMaterial({
      map: texture,
      roughness: 0.45,
      metalness: 0.1,
    });

    const crateMesh = new THREE.Mesh(geometry, material);
    crateMesh.castShadow = true;
    scene.add(crateMesh);

    // 6. Floating Shadow Plane beneath crate
    const shadowGeo = new THREE.PlaneGeometry(2.4, 2.4);
    const shadowMat = new THREE.MeshBasicMaterial({
      color: 0x047857,
      transparent: true,
      opacity: 0.15,
    });
    const shadowMesh = new THREE.Mesh(shadowGeo, shadowMat);
    shadowMesh.rotation.x = -Math.PI / 2;
    shadowMesh.position.y = -0.9;
    scene.add(shadowMesh);

    // 7. Interactive Mouse / Touch Drag Rotation
    let isDragging = false;
    let previousMousePosition = { x: 0, y: 0 };

    const onPointerDown = (e: PointerEvent) => {
      isDragging = true;
      previousMousePosition = { x: e.clientX, y: e.clientY };
    };

    const onPointerMove = (e: PointerEvent) => {
      if (!isDragging) return;
      const deltaX = e.clientX - previousMousePosition.x;
      const deltaY = e.clientY - previousMousePosition.y;

      crateMesh.rotation.y += deltaX * 0.01;
      crateMesh.rotation.x += deltaY * 0.01;

      previousMousePosition = { x: e.clientX, y: e.clientY };
    };

    const onPointerUp = () => {
      isDragging = false;
    };

    const domElement = renderer.domElement;
    domElement.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("pointermove", onPointerMove);
    window.addEventListener("pointerup", onPointerUp);

    // 8. Animation Loop
    let animationFrameId: number;
    let clock = new THREE.Clock();

    const animate = () => {
      animationFrameId = requestAnimationFrame(animate);
      const elapsedTime = clock.getElapsedTime();

      // Idle gentle floating and rotation
      if (!isDragging) {
        crateMesh.rotation.y += 0.008;
        crateMesh.position.y = Math.sin(elapsedTime * 2) * 0.08;
      }

      renderer.render(scene, camera);
    };

    animate();

    // 9. Resize Handler
    const handleResize = () => {
      if (!container) return;
      const newWidth = container.clientWidth || 360;
      camera.aspect = newWidth / height;
      camera.updateProjectionMatrix();
      renderer.setSize(newWidth, height);
    };

    window.addEventListener("resize", handleResize);

    // 10. Clean-up Function: dispose geometries and renderer to prevent WebGL leak
    return () => {
      cancelAnimationFrame(animationFrameId);
      window.removeEventListener("resize", handleResize);
      domElement.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("pointermove", onPointerMove);
      window.removeEventListener("pointerup", onPointerUp);

      geometry.dispose();
      material.dispose();
      texture.dispose();
      shadowGeo.dispose();
      shadowMat.dispose();
      renderer.dispose();

      if (container.contains(domElement)) {
        container.removeChild(domElement);
      }
    };
  }, []);

  return (
    <div
      style={{
        backgroundColor: "#ffffff",
        border: "1px solid #d1fae5",
        borderRadius: "16px",
        padding: "16px",
        boxShadow: "0 4px 6px -1px rgba(0,0,0,0.05)",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "8px" }}>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <span style={{ fontSize: "18px" }}>📦</span>
          <h2 style={{ fontSize: "16px", fontWeight: 700, color: "#065f46" }}>
            Interactive 3D Delivery Package
          </h2>
        </div>
        <span style={{ fontSize: "12px", color: "#6b7280", fontStyle: "italic" }}>
          🖱️ Drag to rotate 360°
        </span>
      </div>

      {/* 3D WebGL Canvas Container */}
      <div
        ref={containerRef}
        style={{
          width: "100%",
          height: "300px",
          cursor: "grab",
          borderRadius: "12px",
          overflow: "hidden",
        }}
      />
    </div>
  );
};


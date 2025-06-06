// Configuration - can be set dynamically
let apiConfig = {
  "baseUrl": "",
  "endpoints": {
    "Configuration_GetApiConfiguration": "api/Configuration",
    "GtfsData_GettAllDataFromStaticFile": "api/GtfsData/StaticData",
    "Route_GetAllRoutes": "api/Route/AllRoutes",
    "Route_GetRouteShape": "api/Route/RouteShape",
    "Statistics_GetHealthStatus": "api/Statistics/health",
    "Statistics_GetCacheStats": "api/Statistics/cache/stats",
    "VehiclePosition_GetCurrentVehiclePositions": "api/VehiclePosition/CurrentPositions",
    "VehiclePosition_GetCurrentVehiclePositionByRoute": "api/VehiclePosition/CurrentPositionByRoute"
  }
};

// Initialize API configuration
async function initializeApiConfig() {
  try {
    // Try to get config from server
    const response = await fetch('/api/Configuration');
    if (response.ok) {
      const config = await response.json();
      apiConfig = { ...apiConfig, ...config };
    }
  } catch (error) {
    console.warn('Could not load API configuration, using defaults');
  }
}

// API helper functions
const api = {
  async getVehiclePositions(routeId) {
    const url = `${apiConfig.baseUrl}${apiConfig.endpoints.VehiclePosition_GetCurrentVehiclePositionByRoute}?routeId=${routeId}`;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return response.json();
  },

  async getRoutes() {
    const url = `${apiConfig.baseUrl}${apiConfig.endpoints.Route_GetAllRoutes}`;
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return response.json();
  }
};

// Global variables
let trackedRoutes = new Map();
let availableRoutes = new Map();
let hasAutoFocused = false;

// Initialize the map centered near Zagreb
const map = L.map('map').setView([45.83, 16.05], 14);

// Add OpenStreetMap tiles
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  maxZoom: 19,
  attribution: '© OpenStreetMap contributors'
}).addTo(map);

// Get DOM elements
const routeSelect = document.getElementById('routeSelect');
const addButton = document.getElementById('addButton');
const statusDiv = document.getElementById('status');
const routesList = document.getElementById('routesList');

// Create different colored markers for different routes
const routeColors = ['#007cff', '#dc3545', '#28a745', '#6f42c1', '#fd7e14', '#e83e8c', '#17a2b8', '#6c757d', '#343a40', '#ffc107'];

function getRouteColor(routeId) {
  const routes = Array.from(trackedRoutes.keys()).sort();
  const index = routes.indexOf(routeId);
  return routeColors[index % routeColors.length];
}

// Fetch and populate available routes
async function loadAvailableRoutes() {
  try {
    showStatus('Loading available routes...', 'loading');
    const routesRaw = await api.getRoutes();

    availableRoutes.clear();
    routeSelect.innerHTML = '<option value="">Select a route...</option>';

    // Each route is a CSV string: route_id,agency_id,route_short_name,route_long_name,route_type,route_color
    routesRaw.forEach(routeLine => {
      const parts = routeLine.split(',');
      if (parts.length < 4) return;
      const routeId = parseInt(parts[0]);
      if (!routeId) return;

      const agencyId = parts[1];
      const shortName = parts[2];
      const longName = parts[3];
      const type = parts.length > 4 ? parseInt(parts[4]) : 3;
      const color = parts.length > 5 ? `#${parts[5]}` : undefined;

      availableRoutes.set(routeId, {
        id: routeId,
        name: shortName,
        longName: longName,
        type: isNaN(type) ? 3 : type,
        color: color,
        textColor: 'white'
      });

      const option = document.createElement('option');
      option.value = routeId;

      // Add emoji based on type
      const typeEmoji = type === 0 ? '🚋' : type === 3 ? '🚌' : '🚐';
      // Display route number and long name without quotes or extra spaces
      const displayName = [shortName, longName].filter(s => s && s !== '""' && s !== "''" && s.trim() !== '').join(' ').trim();
      option.textContent = `${typeEmoji} ${displayName}`;

      if (longName) {
        option.title = longName;
      }

      routeSelect.appendChild(option);
    });

    addButton.disabled = false;
    showStatus(`Loaded ${availableRoutes.size} routes`, 'success');

  } catch (error) {
    console.error('Error loading routes:', error);
    showStatus('Failed to load routes. Check console for details.', 'error');

    routeSelect.innerHTML = '<option value="">Failed to load - enter manually</option>';
    addButton.disabled = false;
  }
}

// Create custom emoji marker for vehicles
function createVehicleMarker(lat, lng, routeId, vehicleData, vehicleIndex) {
  const color = getRouteColor(routeId);
  const routeInfo = availableRoutes.get(routeId);

  // Determine emoji based on route type
  let emoji = '🚐'; // default
  if (routeInfo) {
    emoji = routeInfo.type === 0 ? '🚋' : routeInfo.type === 3 ? '🚌' : '🚐';
  }

  const routeText = routeId.toString().slice(0, 3);

  return L.marker([lat, lng], {
    icon: L.divIcon({
      className: 'vehicle-marker',
      html: `
        <div style="
          background-color: ${routeInfo?.color || color};
          color: ${routeInfo?.textColor || 'white'};
          width: 36px;
          height: 36px;
          border-radius: 50%;
          border: 3px solid white;
          box-shadow: 0 2px 6px rgba(0,0,0,0.3);
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 16px;
          position: relative;
        ">
          <div style="
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            z-index: 1;
          ">${emoji}</div>
          <div style="
            position: absolute;
            bottom: -2px;
            left: 50%;
            transform: translateX(-50%);
            background-color: rgba(0,0,0,0.8);
            color: white;
            font-size: 9px;
            font-weight: bold;
            padding: 1px 3px;
            border-radius: 6px;
            min-width: 16px;
            text-align: center;
            font-family: Arial, sans-serif;
            z-index: 2;
            line-height: 1;
          ">${routeText}</div>
        </div>
      `,
      iconSize: [42, 42],
      iconAnchor: [21, 21]
    })
  }).bindPopup(createVehiclePopup(routeId, vehicleData, vehicleIndex));
}

// Create popup content for vehicle marker
function createVehiclePopup(routeId, vehicleData, vehicleIndex) {
  const routeInfo = availableRoutes.get(routeId);
  const routeName = routeInfo?.name || routeInfo?.longName || '';
  const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Vehicle';

  let popup = `<strong>Route ${routeId}${routeName ? ` - ${routeName}` : ''}</strong><br>`;
  popup += `<strong>${vehicleType} ${vehicleIndex + 1}</strong><br>`;

  // Handle different possible property names from your ASP.NET models
  if (vehicleData.latitude || vehicleData.lat) {
    popup += `Latitude: ${(vehicleData.latitude || vehicleData.lat).toFixed(6)}<br>`;
  }
  if (vehicleData.longitude || vehicleData.lng || vehicleData.lon) {
    popup += `Longitude: ${(vehicleData.longitude || vehicleData.lng || vehicleData.lon).toFixed(6)}<br>`;
  }
  if (vehicleData.speed) {
    popup += `Speed: ${vehicleData.speed} km/h<br>`;
  }
  if (vehicleData.bearing || vehicleData.heading) {
    popup += `Bearing: ${vehicleData.bearing || vehicleData.heading}°<br>`;
  }
  if (vehicleData.vehicleId || vehicleData.id) {
    popup += `Vehicle ID: ${vehicleData.vehicleId || vehicleData.id}<br>`;
  }
  if (vehicleData.timestamp || vehicleData.lastUpdate) {
    const time = new Date(vehicleData.timestamp || vehicleData.lastUpdate);
    popup += `Last Update: ${time.toLocaleTimeString()}<br>`;
  }

  return popup;
}

// Show status message
function showStatus(message, type = 'loading') {
  statusDiv.textContent = message;
  statusDiv.className = `status ${type}`;
  statusDiv.style.display = 'block';

  if (type === 'success' || type === 'error') {
    setTimeout(() => {
      statusDiv.style.display = 'none';
    }, 3000);
  }
}

// Update the routes list display
function updateRoutesList() {
  routesList.innerHTML = '';

  trackedRoutes.forEach((routeData, routeId) => {
    const routeItem = document.createElement('div');
    routeItem.className = `route-item ${routeData.visible ? 'active' : ''}`;

    const visibilityIcon = routeData.visible ? '👁️' : '👁️‍🗨️';
    const routeInfo = availableRoutes.get(routeId);
    const typeEmoji = routeInfo && routeInfo.type === 0 ? '🚋' : routeInfo && routeInfo.type === 3 ? '🚌' : '🚐';

    routeItem.innerHTML = `
      <div class="visibility-toggle">${visibilityIcon}</div>
      <div class="route-info">
        <div class="route-number">${typeEmoji} Route ${routeId}</div>
        ${routeData.name ? `<div class="route-name">${routeData.name}</div>` : ''}
        <div class="vehicle-count">
          ${routeData.visible ? `${routeData.vehicleCount} vehicles` : 'Hidden'}
        </div>
      </div>
      <button class="remove-button" onclick="removeRoute(${routeId})" title="Remove Route">×</button>
    `;

    const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Other';
    if (routeData.name) {
      routeItem.title = `${routeId} - ${routeData.name} (${vehicleType})`;
    }

    routeItem.addEventListener('click', (e) => {
      if (e.target.classList.contains('remove-button')) return;
      toggleRouteVisibility(routeId);
    });

    routesList.appendChild(routeItem);
  });
}

// Toggle route visibility
function toggleRouteVisibility(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData) return;

  routeData.visible = !routeData.visible;

  if (routeData.visible) {
    startTrackingRoute(routeId);
  } else {
    stopTrackingRoute(routeId);
  }

  updateRoutesList();
}

// Add a new route to track
function addRoute(routeId) {
  if (trackedRoutes.has(routeId)) {
    showStatus(`Route ${routeId} is already added`, 'error');
    return;
  }

  const routeInfo = availableRoutes.get(routeId);
  const routeName = routeInfo ? (routeInfo.name || routeInfo.longName) : '';
  const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Other';

  trackedRoutes.set(routeId, {
    markers: [],
    interval: null,
    vehicleCount: 0,
    visible: true,
    name: routeName
  });

  startTrackingRoute(routeId);
  updateRoutesList();
  showStatus(`${vehicleType} ${routeId}${routeName ? ` (${routeName})` : ''} added and tracking started`, 'success');

  routeSelect.value = '';
}

// Remove a route
function removeRoute(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData) return;

  if (routeData.interval) {
    clearInterval(routeData.interval);
  }

  routeData.markers.forEach(marker => map.removeLayer(marker));
  trackedRoutes.delete(routeId);

  updateRoutesList();
  showStatus(`Route ${routeId} removed`, 'success');
}

window.removeRoute = removeRoute;

// Function to fetch and plot vehicle positions for a specific route
async function fetchAndRenderVehicles(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData || !routeData.visible) return;

  try {
    const vehicles = await api.getVehiclePositions(routeId);

    // Clear existing markers for this route
    routeData.markers.forEach(marker => map.removeLayer(marker));
    routeData.markers = [];

    if (!vehicles || vehicles.length === 0) {
      routeData.vehicleCount = 0;
      updateRoutesList();
      return;
    }

    let plotted = 0;

    // Plot new markers - handle different possible property names
    vehicles.forEach((vehicle, index) => {
      const lat = vehicle.latitude || vehicle.lat;
      const lng = vehicle.longitude || vehicle.lng || vehicle.lon;
      const hasPosition = (vehicle.hasLatitude && vehicle.hasLongitude) || (lat && lng);

      if (hasPosition && lat && lng) {
        const marker = createVehicleMarker(lat, lng, routeId, vehicle, index);
        marker.addTo(map);
        routeData.markers.push(marker);
        plotted++;
      }
    });

    routeData.vehicleCount = plotted;
    updateRoutesList();

  } catch (error) {
    console.error(`Error fetching vehicle positions for route ${routeId}:`, error);
    routeData.vehicleCount = 0;
    updateRoutesList();
  }
}

// Start tracking a specific route
function startTrackingRoute(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData || routeData.interval) return;

  fetchAndRenderVehicles(routeId);

  routeData.interval = setInterval(() => {
    fetchAndRenderVehicles(routeId);
  }, 5000);

  updateRoutesList();
}

// Stop tracking a specific route
function stopTrackingRoute(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData) return;

  if (routeData.interval) {
    clearInterval(routeData.interval);
    routeData.interval = null;
  }

  routeData.vehicleCount = 0;
  routeData.markers.forEach(marker => map.removeLayer(marker));
  routeData.markers = [];

  updateRoutesList();
}

// Event listeners
addButton.addEventListener('click', () => {
  const routeId = parseInt(routeSelect.value);
  if (isNaN(routeId) || routeId <= 0) {
    showStatus('Please select a valid route', 'error');
    return;
  }
  addRoute(routeId);
});

routeSelect.addEventListener('keypress', (e) => {
  if (e.key === 'Enter' && routeSelect.value) {
    addButton.click();
  }
});

// Initialize the application
async function initialize() {
  await initializeApiConfig();
  await loadAvailableRoutes();

  // Initialize with a default route if available
  const defaultRouteId = 206;
  if (availableRoutes.has(defaultRouteId)) {
    routeSelect.value = defaultRouteId;
    addRoute(defaultRouteId);
  }
}

// Start the application
initialize();

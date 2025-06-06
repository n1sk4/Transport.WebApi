// Configuration - matches your actual API endpoints
let apiConfig = {
  "baseUrl": "",
  "endpoints": {
    "Configuration_GetApiConfiguration": "api/Configuration",
    "Route_GetAllRoutes": "api/Route/AllRoutes",
    "Route_GetRouteShape": "api/Route/RouteShape",
    "Statistics_GetHealthStatus": "api/Statistics/health",
    "VehiclePosition_GetCurrentVehiclePositions": "api/VehiclePosition/CurrentPositionsEnhanced",
    "VehiclePosition_GetCurrentVehiclePositionByRoute": "api/VehiclePosition/CurrentPositionByRouteEnhanced"
  }
};

// Initialize API configuration
async function initializeApiConfig() {
  try {
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
      if (response.status === 404) {
        return null; // No vehicles for this route
      }
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return response.json();
  },

  async getAllVehiclePositions() {
    const url = `${apiConfig.baseUrl}${apiConfig.endpoints.VehiclePosition_GetCurrentVehiclePositions}`;
    const response = await fetch(url);
    if (!response.ok) {
      if (response.status === 404) {
        return null; // No vehicles available
      }
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
  },

  async getRouteShape(routeId) {
    const url = `${apiConfig.baseUrl}${apiConfig.endpoints.Route_GetRouteShape}?routeId=${routeId}`;
    const response = await fetch(url);
    if (!response.ok) {
      if (response.status === 404) {
        return null; // No shape data for this route
      }
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    return response.json();
  }
};

// Global variables
let trackedRoutes = new Map();
let availableRoutes = new Map();
let allVehiclesMarkers = [];
let showingAllVehicles = false;

// Initialize the map centered on Zagreb
const map = L.map('map').setView([45.8150, 15.9819], 12);

// Add OpenStreetMap tiles
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
  maxZoom: 19,
  attribution: '© OpenStreetMap contributors'
}).addTo(map);

// Get DOM elements
const routeSelect = document.getElementById('routeSelect');
const addButton = document.getElementById('addButton');
const showAllVehiclesButton = document.getElementById('showAllVehicles');
const clearAllButton = document.getElementById('clearAll');
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
    const routesData = await api.getRoutes();

    availableRoutes.clear();
    routeSelect.innerHTML = '<option value="">Select a route...</option>';

    // Process the JsonSerializedRoutes array from your API
    routesData.forEach(route => {
      const routeId = parseInt(route.routeId);
      if (!routeId) return;

      // Clean up the route names
      const shortName = route.routeShortName?.replace(/"/g, '').trim() || routeId.toString();
      const longName = route.routeLongName?.replace(/"/g, '').trim() || '';
      const routeType = parseInt(route.routeType) || 3;

      availableRoutes.set(routeId, {
        id: routeId,
        shortName: shortName,
        longName: longName,
        type: routeType
      });

      const option = document.createElement('option');
      option.value = routeId;

      // Add emoji based on type
      const typeEmoji = routeType === 0 ? '🚋' : routeType === 3 ? '🚌' : '🚐';

      // Display format: emoji + short name + long name (if different and exists)
      let displayName = `${typeEmoji} ${shortName}`;
      if (longName && longName !== shortName && longName.length > 0) {
        displayName += ` - ${longName}`;
      }

      option.textContent = displayName;
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
    routeSelect.innerHTML = '<option value="">Failed to load routes</option>';
    addButton.disabled = false;
  }
}

// Create custom emoji marker for vehicles
function createVehicleMarker(lat, lng, routeType, routeId, routeShortName) {
  const color = getRouteColor(routeId);

  // Determine emoji based on route type
  let emoji = '🚐'; // default
  if (routeType === 0) {
    emoji = '🚋'; // tram
  } else if (routeType === 3) {
    emoji = '🚌'; // bus
  } else if (routeType === 1) {
    emoji = '🚇'; // subway
  } else if (routeType === 2) {
    emoji = '🚆'; // rail
  }

  const routeText = routeShortName || routeId.toString().slice(-3);

  return L.marker([lat, lng], {
    icon: L.divIcon({
      className: 'vehicle-marker',
      html: `
        <div style="
          background-color: ${color};
          color: white;
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
  }).bindPopup(createVehiclePopup(routeId, routeShortName, routeType));
}

// Create popup content for vehicle marker
function createVehiclePopup(routeId, routeShortName, routeType) {
  const vehicleType = routeType === 0 ? 'Tram' : routeType === 3 ? 'Bus' : routeType === 1 ? 'Subway' : routeType === 2 ? 'Rail' : 'Vehicle';

  let popup = `<strong>Route ${routeShortName || routeId}</strong><br>`;
  popup += `<strong>${vehicleType}</strong><br>`;
  popup += `Route ID: ${routeId}<br>`;
  popup += `Last Update: ${new Date().toLocaleTimeString()}`;

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
    const routeName = routeInfo?.shortName || routeId.toString();

    routeItem.innerHTML = `
      <div class="visibility-toggle">${visibilityIcon}</div>
      <div class="route-info">
        <div class="route-number">${typeEmoji} ${routeName}</div>
        ${routeInfo?.longName ? `<div class="route-name">${routeInfo.longName}</div>` : ''}
        <div class="vehicle-count">
          ${routeData.visible ? `${routeData.vehicleCount} vehicles` : 'Hidden'}
        </div>
      </div>
      <button class="remove-button" onclick="removeRoute(${routeId})" title="Remove Route">×</button>
    `;

    const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Other';
    routeItem.title = `${routeName} (${vehicleType})`;

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
  const routeName = routeInfo?.shortName || routeId.toString();
  const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Vehicle';

  trackedRoutes.set(routeId, {
    markers: [],
    interval: null,
    vehicleCount: 0,
    visible: true
  });

  startTrackingRoute(routeId);
  updateRoutesList();
  showStatus(`${vehicleType} ${routeName} added and tracking started`, 'success');

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
    const vehicleData = await api.getVehiclePositions(routeId);

    // Clear existing markers for this route
    routeData.markers.forEach(marker => map.removeLayer(marker));
    routeData.markers = [];

    if (!vehicleData || !vehicleData.vehicles || vehicleData.vehicles.length === 0) {
      routeData.vehicleCount = 0;
      updateRoutesList();
      return;
    }

    let plotted = 0;

    // Plot vehicles using the enhanced data structure
    vehicleData.vehicles.forEach((vehicle) => {
      if (vehicle.latitude && vehicle.longitude) {
        const marker = createVehicleMarker(
          vehicle.latitude,
          vehicle.longitude,
          vehicleData.routeType,
          vehicleData.routeId,
          vehicleData.routeShortName
        );
        marker.addTo(map);
        routeData.markers.push(marker);
        plotted++;
      }
    });

    routeData.vehicleCount = plotted;
    updateRoutesList();

    // Auto-focus on first route added
    if (plotted > 0 && trackedRoutes.size === 1) {
      const group = new L.featureGroup(routeData.markers);
      map.fitBounds(group.getBounds().pad(0.1));
    }

  } catch (error) {
    console.error(`Error fetching vehicle positions for route ${routeId}:`, error);
    routeData.vehicleCount = 0;
    updateRoutesList();
  }
}

// Show all vehicles regardless of route
async function showAllVehicles() {
  try {
    showStatus('Loading all vehicles...', 'loading');

    // Clear existing markers
    clearAllMarkers();

    const allVehicleData = await api.getAllVehiclePositions();

    if (!allVehicleData || !Array.isArray(allVehicleData)) {
      showStatus('No vehicles currently available', 'error');
      return;
    }

    let totalVehicles = 0;
    allVehiclesMarkers = [];

    // Process all routes and their vehicles using enhanced data structure
    allVehicleData.forEach(routeData => {
      if (routeData.vehicles && Array.isArray(routeData.vehicles)) {
        routeData.vehicles.forEach(vehicle => {
          if (vehicle.latitude && vehicle.longitude) {
            const marker = createVehicleMarker(
              vehicle.latitude,
              vehicle.longitude,
              routeData.routeType,
              routeData.routeId,
              routeData.routeShortName
            );
            marker.addTo(map);
            allVehiclesMarkers.push(marker);
            totalVehicles++;
          }
        });
      }
    });

    showingAllVehicles = true;
    showAllVehiclesButton.textContent = 'Hide All Vehicles';

    if (totalVehicles > 0) {
      showStatus(`Showing ${totalVehicles} vehicles across all routes`, 'success');
      // Fit map to show all vehicles
      if (allVehiclesMarkers.length > 0) {
        const group = new L.featureGroup(allVehiclesMarkers);
        map.fitBounds(group.getBounds().pad(0.05));
      }
    } else {
      showStatus('No vehicles found', 'error');
    }

  } catch (error) {
    console.error('Error fetching all vehicles:', error);
    showStatus('Failed to load vehicles', 'error');
  }
}
async function showAllVehicles() {
  try {
    showStatus('Loading all vehicles...', 'loading');

    // Clear existing markers
    clearAllMarkers();

    const allVehicleData = await api.getAllVehiclePositions();

    if (!allVehicleData) {
      showStatus('No vehicles currently available', 'error');
      return;
    }

    let totalVehicles = 0;
    allVehiclesMarkers = [];

    // Process all routes and their vehicles
    Object.keys(allVehicleData).forEach(routeId => {
      const positions = allVehicleData[routeId];
      if (Array.isArray(positions)) {
        positions.forEach(positionStr => {
          const coords = positionStr.split(',');
          if (coords.length >= 2) {
            const lat = parseFloat(coords[0]);
            const lng = parseFloat(coords[1]);

            if (!isNaN(lat) && !isNaN(lng)) {
              const marker = createVehicleMarker(lat, lng, routeId, true);
              marker.addTo(map);
              allVehiclesMarkers.push(marker);
              totalVehicles++;
            }
          }
        });
      }
    });

    showingAllVehicles = true;
    showAllVehiclesButton.textContent = 'Hide All Vehicles';

    if (totalVehicles > 0) {
      showStatus(`Showing ${totalVehicles} vehicles across all routes`, 'success');
      // Fit map to show all vehicles
      if (allVehiclesMarkers.length > 0) {
        const group = new L.featureGroup(allVehiclesMarkers);
        map.fitBounds(group.getBounds().pad(0.05));
      }
    } else {
      showStatus('No vehicles found', 'error');
    }

  } catch (error) {
    console.error('Error fetching all vehicles:', error);
    showStatus('Failed to load vehicles', 'error');
  }
}

// Hide all vehicles
function hideAllVehicles() {
  allVehiclesMarkers.forEach(marker => map.removeLayer(marker));
  allVehiclesMarkers = [];
  showingAllVehicles = false;
  showAllVehiclesButton.textContent = 'Show All Vehicles';
  showStatus('All vehicles hidden', 'success');
}

// Clear all markers and stop tracking
function clearAllRoutes() {
  // Stop all intervals
  trackedRoutes.forEach((routeData, routeId) => {
    if (routeData.interval) {
      clearInterval(routeData.interval);
    }
    routeData.markers.forEach(marker => map.removeLayer(marker));
  });

  // Clear all vehicles markers
  allVehiclesMarkers.forEach(marker => map.removeLayer(marker));
  allVehiclesMarkers = [];

  trackedRoutes.clear();
  showingAllVehicles = false;
  showAllVehiclesButton.textContent = 'Show All Vehicles';

  updateRoutesList();
  showStatus('All routes cleared', 'success');
}

// Clear all markers
function clearAllMarkers() {
  // Clear route-specific markers
  trackedRoutes.forEach(routeData => {
    routeData.markers.forEach(marker => map.removeLayer(marker));
    routeData.markers = [];
    routeData.vehicleCount = 0;
  });

  // Clear all vehicles markers
  allVehiclesMarkers.forEach(marker => map.removeLayer(marker));
  allVehiclesMarkers = [];

  updateRoutesList();
}

// Start tracking a specific route
function startTrackingRoute(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData || routeData.interval) return;

  // Hide all vehicles view if active
  if (showingAllVehicles) {
    hideAllVehicles();
  }

  fetchAndRenderVehicles(routeId);

  routeData.interval = setInterval(() => {
    fetchAndRenderVehicles(routeId);
  }, 10000); // Update every 10 seconds

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

showAllVehiclesButton.addEventListener('click', () => {
  if (showingAllVehicles) {
    hideAllVehicles();
  } else {
    showAllVehicles();
  }
});

clearAllButton.addEventListener('click', () => {
  clearAllRoutes();
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

  showStatus('Ready! Select a route to start tracking vehicles.', 'success');
}

// Start the application
initialize();

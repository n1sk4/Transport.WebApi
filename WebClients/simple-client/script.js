// Configuration and API endpoints
const API_CONFIG = {
  baseUrl: '', // Will be set dynamically or use current domain
  endpoints: {
    allRoutes: 'api/Route/AllRoutes',
    currentPositionsEnhanced: 'api/VehiclePosition/CurrentPositionsEnhanced',
    currentPositionByRouteEnhanced: 'api/VehiclePosition/CurrentPositionByRouteEnhanced'
  }
};

// Global state
let map;
let allRoutes = [];
let trackedRoutes = new Map(); // routeId -> {routeData, markers, visible, interval}
let allVehiclesMode = false;

// Storage keys
const STORAGE_KEYS = {
  routes: 'zet_routes_cache_v2', // Changed version to force cache refresh
  cacheTimestamp: 'zet_routes_cache_timestamp_v2'
};

// Cache duration (24 hours)
const CACHE_DURATION = 24 * 60 * 60 * 1000;

// Initialize the application
document.addEventListener('DOMContentLoaded', async () => {
  initializeMap();
  await loadRoutes();
  setupEventListeners();
  showStatus('Ready! Select a route to start tracking.', 'success');
});

// Initialize Leaflet map
function initializeMap() {
  // Center on Zagreb
  map = L.map('map').setView([45.8150, 15.9819], 12);

  // Add OpenStreetMap tiles
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    maxZoom: 19,
    attribution: '© OpenStreetMap contributors'
  }).addTo(map);
}

// API helper functions
const api = {
  async fetchAllRoutes() {
    const startTime = performance.now();
    const response = await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.allRoutes}`);
    if (!response.ok) {
      throw new Error(`Failed to fetch routes: ${response.status} ${response.statusText}`);
    }
    const data = await response.json();
    const endTime = performance.now();
    console.log(`Routes API call took ${(endTime - startTime).toFixed(2)}ms`);
    return data;
  },

  async fetchAllVehicles() {
    const startTime = performance.now();
    const response = await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.currentPositionsEnhanced}`);
    if (!response.ok) {
      if (response.status === 404) {
        console.log('No vehicles available (404)');
        return [];
      }
      throw new Error(`Failed to fetch all vehicles: ${response.status} ${response.statusText}`);
    }
    const data = await response.json();
    const endTime = performance.now();
    console.log(`All vehicles API call took ${(endTime - startTime).toFixed(2)}ms, returned ${data.length} routes`);
    return data;
  },

  async fetchVehiclesByRoute(routeId) {
    const startTime = performance.now();
    const response = await fetch(`${API_CONFIG.baseUrl}${API_CONFIG.endpoints.currentPositionByRouteEnhanced}?routeId=${routeId}`);
    if (!response.ok) {
      if (response.status === 404) {
        console.log(`No vehicles available for route ${routeId} (404)`);
        return null;
      }
      throw new Error(`Failed to fetch vehicles for route ${routeId}: ${response.status} ${response.statusText}`);
    }
    const data = await response.json();
    const endTime = performance.now();
    const vehicleCount = data?.vehicles?.length || 0;
    console.log(`Route ${routeId} API call took ${(endTime - startTime).toFixed(2)}ms, returned ${vehicleCount} vehicles`);
    return data;
  }
};

// Load and cache routes
async function loadRoutes() {
  try {
    showStatus('Loading routes...', 'loading');

    // Check cache first
    const cachedRoutes = getCachedRoutes();
    if (cachedRoutes) {
      allRoutes = cachedRoutes;
      populateRouteDropdown();
      showStatus(`Loaded ${allRoutes.length} routes from cache`, 'success');
      return;
    }

    // Fetch from API
    const routesData = await api.fetchAllRoutes();
    console.log('Raw routes data sample:', routesData.slice(0, 3)); // Debug log

    allRoutes = routesData.map(route => {
      // Make sure we parse the routeType correctly - it comes as string
      const routeType = parseInt(route.routeType, 10);
      const parsedRoute = {
        routeId: route.routeId,
        shortName: cleanRouteString(route.routeShortName),
        longName: cleanRouteString(route.routeLongName),
        routeType: isNaN(routeType) ? 3 : routeType // Default to bus if parsing fails
      };
      console.log(`Route ${route.routeId}: original="${route.routeType}", parsed=${parsedRoute.routeType}`); // Debug log
      return parsedRoute;
    });

    // Cache the routes
    cacheRoutes(allRoutes);
    populateRouteDropdown();
    showStatus(`Loaded ${allRoutes.length} routes`, 'success');

  } catch (error) {
    console.error('Error loading routes:', error);
    showStatus('Failed to load routes. Please refresh the page.', 'error');
  }
}

// Cache management
function getCachedRoutes() {
  try {
    const timestamp = sessionStorage.getItem(STORAGE_KEYS.cacheTimestamp);
    const cachedData = sessionStorage.getItem(STORAGE_KEYS.routes);

    if (!timestamp || !cachedData) return null;

    const age = Date.now() - parseInt(timestamp);
    if (age > CACHE_DURATION) {
      // Cache expired
      sessionStorage.removeItem(STORAGE_KEYS.routes);
      sessionStorage.removeItem(STORAGE_KEYS.cacheTimestamp);
      return null;
    }

    return JSON.parse(cachedData);
  } catch (error) {
    console.error('Error reading cache:', error);
    return null;
  }
}

function cacheRoutes(routes) {
  try {
    sessionStorage.setItem(STORAGE_KEYS.routes, JSON.stringify(routes));
    sessionStorage.setItem(STORAGE_KEYS.cacheTimestamp, Date.now().toString());
  } catch (error) {
    console.error('Error caching routes:', error);
  }
}

// Utility functions
function cleanRouteString(str) {
  return str ? str.replace(/['"]/g, '').trim() : '';
}

function getVehicleEmoji(routeType) {
  switch (routeType) {
    case 0: return '🚋'; // Tram
    case 1: return '🚇'; // Subway
    case 2: return '🚆'; // Rail
    case 3: return '🚌'; // Bus
    default: return '🚐'; // Other
  }
}

function getVehicleColor(routeType, isAllVehiclesMode = false) {
  if (isAllVehiclesMode) {
    switch (routeType) {
      case 0: return '#ff6b35'; // Orange for trams
      case 1: return '#6b5b95'; // Purple for subway
      case 2: return '#88d8b0'; // Green for rail
      case 3: return '#2196f3'; // Blue for buses
      default: return '#78909c'; // Gray for other
    }
  } else {
    // Route-specific colors
    const colors = ['#f44336', '#e91e63', '#9c27b0', '#673ab7', '#3f51b5', '#2196f3', '#03a9f4', '#00bcd4', '#009688', '#4caf50', '#8bc34a', '#cddc39', '#ffeb3b', '#ffc107', '#ff9800', '#ff5722'];
    const routeList = Array.from(trackedRoutes.keys()).sort();
    const index = routeList.indexOf(parseInt(routeType)) % colors.length;
    return colors[index];
  }
}

// UI functions
function populateRouteDropdown() {
  const dropdown = document.getElementById('routeDropdown');
  const trackBtn = document.getElementById('trackRouteBtn');

  dropdown.innerHTML = '<option value="">Select a route...</option>';

  console.log('Populating dropdown. Total routes:', allRoutes.length);
  console.log('First 5 routes with types:', allRoutes.slice(0, 5).map(r => ({
    id: r.routeId,
    name: r.shortName,
    type: r.routeType,
    emoji: getVehicleEmoji(r.routeType)
  })));

  // Sort routes by route type and then by short name
  const sortedRoutes = [...allRoutes].sort((a, b) => {
    if (a.routeType !== b.routeType) {
      return a.routeType - b.routeType;
    }
    return a.shortName.localeCompare(b.shortName, undefined, { numeric: true });
  });

  // Count routes by type for debugging
  const typeCounts = {};
  sortedRoutes.forEach(route => {
    typeCounts[route.routeType] = (typeCounts[route.routeType] || 0) + 1;
  });
  console.log('Route type distribution:', typeCounts);

  sortedRoutes.forEach(route => {
    const option = document.createElement('option');
    option.value = route.routeId;

    const emoji = getVehicleEmoji(route.routeType);
    const displayName = `${emoji} ${route.shortName}${route.longName ? ` - ${route.longName}` : ''}`;

    option.textContent = displayName;
    option.title = `Route ${route.shortName} (${getVehicleTypeName(route.routeType)}) - Type: ${route.routeType}`;

    dropdown.appendChild(option);
  });

  trackBtn.disabled = false;
}

function getVehicleTypeName(routeType) {
  switch (routeType) {
    case 0: return 'Tram';
    case 1: return 'Subway';
    case 2: return 'Rail';
    case 3: return 'Bus';
    default: return 'Vehicle';
  }
}

function showStatus(message, type = 'info') {
  const statusEl = document.getElementById('statusMessage');
  statusEl.textContent = message;
  statusEl.className = `status-message ${type}`;
  statusEl.classList.remove('hidden');

  if (type === 'success' || type === 'error') {
    setTimeout(() => {
      statusEl.classList.add('hidden');
    }, 3000);
  }
}

// Vehicle marker creation
function createVehicleMarker(vehicle, routeData, isAllVehiclesMode = false) {
  const { latitude, longitude } = vehicle;
  const { routeType, routeId, routeShortName } = routeData;

  const emoji = getVehicleEmoji(routeType);
  const color = getVehicleColor(routeType, isAllVehiclesMode);
  const routeText = routeShortName || routeId.toString();

  const marker = L.marker([latitude, longitude], {
    icon: L.divIcon({
      className: 'custom-vehicle-marker',
      html: `
                <div class="vehicle-marker" style="background-color: ${color};">
                    <div class="vehicle-emoji">${emoji}</div>
                    <div class="vehicle-route">${routeText}</div>
                </div>
            `,
      iconSize: [40, 40],
      iconAnchor: [20, 20]
    })
  });

  // Add popup
  const popupContent = `
        <div class="vehicle-popup">
            <h4>${emoji} Route ${routeShortName || routeId}</h4>
            <p><strong>Type:</strong> ${getVehicleTypeName(routeType)}</p>
            <p><strong>Position:</strong> ${latitude.toFixed(6)}, ${longitude.toFixed(6)}</p>
            <p><strong>Last Update:</strong> ${new Date().toLocaleTimeString()}</p>
        </div>
    `;

  marker.bindPopup(popupContent);

  return marker;
}

// Route tracking functions
async function trackRoute(routeId) {
  if (trackedRoutes.has(routeId)) {
    showStatus('Route is already being tracked', 'warning');
    return;
  }

  const startTime = performance.now();
  console.log(`Starting to track route ${routeId}...`);

  const route = allRoutes.find(r => r.routeId === routeId);
  if (!route) {
    showStatus('Route not found', 'error');
    return;
  }

  // Show immediate feedback
  showStatus(`Loading vehicles for route ${route.shortName}...`, 'loading');

  // Exit all vehicles mode if active
  if (allVehiclesMode) {
    exitAllVehiclesMode();
  }

  const routeData = {
    ...route,
    markers: [],
    visible: true,
    interval: null,
    vehicleCount: 0
  };

  trackedRoutes.set(parseInt(routeId), routeData);
  updateTrackedRoutesList(); // Show route in list immediately, even with 0 vehicles

  try {
    // Start tracking immediately
    await updateRouteVehicles(parseInt(routeId));

    // Set up interval for updates
    routeData.interval = setInterval(() => updateRouteVehicles(parseInt(routeId)), 10000);

    const endTime = performance.now();
    console.log(`Route ${routeId} loaded in ${(endTime - startTime).toFixed(2)}ms`);

    const emoji = getVehicleEmoji(route.routeType);
    showStatus(`${emoji} Route ${route.shortName} is now being tracked`, 'success');

  } catch (error) {
    console.error(`Error tracking route ${routeId}:`, error);
    showStatus(`Failed to load route ${route.shortName}`, 'error');
    // Remove from tracked routes if failed
    trackedRoutes.delete(parseInt(routeId));
    updateTrackedRoutesList();
  }

  // Clear dropdown selection
  document.getElementById('routeDropdown').value = '';
}

async function updateRouteVehicles(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData || !routeData.visible) return;

  const fetchStart = performance.now();
  console.log(`Fetching vehicles for route ${routeId}...`);

  try {
    const vehicleData = await api.fetchVehiclesByRoute(routeId);
    const fetchEnd = performance.now();
    console.log(`API call for route ${routeId} took ${(fetchEnd - fetchStart).toFixed(2)}ms`);

    const renderStart = performance.now();

    // Clear existing markers
    routeData.markers.forEach(marker => map.removeLayer(marker));
    routeData.markers = [];

    if (!vehicleData || !vehicleData.vehicles || vehicleData.vehicles.length === 0) {
      routeData.vehicleCount = 0;
      updateTrackedRoutesList();
      console.log(`No vehicles found for route ${routeId}`);
      return;
    }

    // Batch add markers for better performance
    const markersToAdd = [];
    vehicleData.vehicles.forEach(vehicle => {
      if (vehicle.latitude && vehicle.longitude) {
        const marker = createVehicleMarker(vehicle, vehicleData, false);
        markersToAdd.push(marker);
        routeData.markers.push(marker);
      }
    });

    // Add all markers at once
    markersToAdd.forEach(marker => marker.addTo(map));

    routeData.vehicleCount = markersToAdd.length;
    updateTrackedRoutesList();

    const renderEnd = performance.now();
    console.log(`Rendered ${markersToAdd.length} markers for route ${routeId} in ${(renderEnd - renderStart).toFixed(2)}ms`);

  } catch (error) {
    console.error(`Error updating vehicles for route ${routeId}:`, error);
    // Check if it's a network timeout
    if (error.message.includes('Failed to fetch') || error.name === 'TypeError') {
      console.log('Possible network timeout or CORS issue');
      showStatus('Network timeout - trying again...', 'warning');
    }
    routeData.vehicleCount = 0;
    updateTrackedRoutesList();
  }
}

function stopTrackingRoute(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData) return;

  // Clear interval
  if (routeData.interval) {
    clearInterval(routeData.interval);
  }

  // Remove markers
  routeData.markers.forEach(marker => map.removeLayer(marker));

  // Remove from tracked routes
  trackedRoutes.delete(routeId);

  updateTrackedRoutesList();

  const route = allRoutes.find(r => r.routeId === routeId.toString());
  if (route) {
    const emoji = getVehicleEmoji(route.routeType);
    showStatus(`${emoji} Route ${route.shortName} stopped tracking`, 'info');
  }
}

function toggleRouteVisibility(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData) return;

  routeData.visible = !routeData.visible;

  if (routeData.visible) {
    // Show markers and resume updates
    updateRouteVehicles(routeId);
    if (!routeData.interval) {
      routeData.interval = setInterval(() => updateRouteVehicles(routeId), 10000);
    }
  } else {
    // Hide markers and pause updates
    routeData.markers.forEach(marker => map.removeLayer(marker));
    routeData.markers = [];
    if (routeData.interval) {
      clearInterval(routeData.interval);
      routeData.interval = null;
    }
  }

  updateTrackedRoutesList();
}

// All vehicles mode
async function showAllVehicles() {
  if (allVehiclesMode) {
    exitAllVehiclesMode();
    return;
  }

  try {
    showStatus('Loading all vehicles...', 'loading');

    // Stop tracking individual routes
    trackedRoutes.forEach((routeData, routeId) => {
      if (routeData.interval) {
        clearInterval(routeData.interval);
        routeData.interval = null;
      }
      routeData.markers.forEach(marker => map.removeLayer(marker));
      routeData.markers = [];
    });

    // Fetch all vehicles
    const allVehiclesData = await api.fetchAllVehicles();

    if (!allVehiclesData || allVehiclesData.length === 0) {
      showStatus('No vehicles currently available', 'warning');
      return;
    }

    // Clear tracked routes and populate with all vehicles data
    trackedRoutes.clear();
    let totalVehicles = 0;

    allVehiclesData.forEach(vehicleRouteData => {
      if (vehicleRouteData.vehicles && vehicleRouteData.vehicles.length > 0) {
        const markers = [];

        vehicleRouteData.vehicles.forEach(vehicle => {
          const marker = createVehicleMarker(vehicle, vehicleRouteData, true);
          marker.addTo(map);
          markers.push(marker);
          totalVehicles++;
        });

        // Find the corresponding route info from our cached routes
        const routeInfo = allRoutes.find(r => r.routeId === vehicleRouteData.routeId);

        // Create the route data object for the tracked routes list
        const routeData = {
          routeId: vehicleRouteData.routeId,
          shortName: vehicleRouteData.routeShortName || (routeInfo ? routeInfo.shortName : vehicleRouteData.routeId),
          longName: vehicleRouteData.routeLongName || (routeInfo ? routeInfo.longName : ''),
          routeType: vehicleRouteData.routeType !== undefined ? vehicleRouteData.routeType : (routeInfo ? routeInfo.routeType : 3),
          markers,
          visible: true,
          interval: null,
          vehicleCount: vehicleRouteData.vehicles.length
        };

        console.log(`Adding route ${vehicleRouteData.routeId} to tracked list:`, {
          shortName: routeData.shortName,
          routeType: routeData.routeType,
          vehicleCount: routeData.vehicleCount
        });

        trackedRoutes.set(parseInt(vehicleRouteData.routeId), routeData);
      }
    });

    allVehiclesMode = true;
    updateShowAllVehiclesButton();
    updateTrackedRoutesList();

    // Fit map to show all vehicles
    const allMarkers = Array.from(trackedRoutes.values()).flatMap(route => route.markers);
    if (allMarkers.length > 0) {
      const group = new L.featureGroup(allMarkers);
      map.fitBounds(group.getBounds().pad(0.05));
    }

    console.log(`Show all vehicles complete. Tracked routes: ${trackedRoutes.size}, Total vehicles: ${totalVehicles}`);
    showStatus(`Showing ${totalVehicles} vehicles across ${trackedRoutes.size} routes`, 'success');

  } catch (error) {
    console.error('Error showing all vehicles:', error);
    showStatus('Failed to load all vehicles', 'error');
  }
}

function exitAllVehiclesMode() {
  // Clear all markers
  trackedRoutes.forEach(routeData => {
    routeData.markers.forEach(marker => map.removeLayer(marker));
  });

  trackedRoutes.clear();
  allVehiclesMode = false;

  updateShowAllVehiclesButton();
  updateTrackedRoutesList();

  showStatus('Exited all vehicles mode', 'info');
}

function updateShowAllVehiclesButton() {
  const btn = document.getElementById('showAllVehiclesBtn');
  btn.textContent = allVehiclesMode ? 'Exit All Vehicles' : 'Show All Vehicles';
  btn.className = allVehiclesMode ? 'btn btn-warning' : 'btn btn-secondary';
}

// UI update functions
function updateTrackedRoutesList() {
  const listContainer = document.getElementById('trackedRoutesList');

  console.log(`Updating tracked routes list. Count: ${trackedRoutes.size}, All vehicles mode: ${allVehiclesMode}`);

  if (trackedRoutes.size === 0) {
    listContainer.innerHTML = `
            <div class="empty-state">
                <p>No routes tracked yet</p>
                <small>Select a route above to start tracking</small>
            </div>
        `;
    return;
  }

  const routeEntries = Array.from(trackedRoutes.entries()).sort((a, b) => {
    // Sort by route type first (trams before buses), then by name
    const [, routeDataA] = a;
    const [, routeDataB] = b;

    if (routeDataA.routeType !== routeDataB.routeType) {
      return routeDataA.routeType - routeDataB.routeType;
    }

    return routeDataA.shortName.localeCompare(routeDataB.shortName, undefined, { numeric: true });
  });

  console.log('Route entries for list:', routeEntries.map(([id, data]) => ({
    id,
    shortName: data.shortName,
    routeType: data.routeType,
    vehicleCount: data.vehicleCount
  })));

  listContainer.innerHTML = routeEntries.map(([routeId, routeData]) => {
    const emoji = getVehicleEmoji(routeData.routeType);
    const visibilityIcon = routeData.visible ? '👁️' : '👁️‍🗨️';
    const statusText = routeData.visible ?
      `${routeData.vehicleCount} vehicle${routeData.vehicleCount !== 1 ? 's' : ''}` :
      'Hidden';

    return `
            <div class="route-item ${routeData.visible ? 'visible' : 'hidden'}">
                <div class="route-header">
                    <div class="route-info">
                        <span class="route-name">${emoji} ${routeData.shortName}</span>
                        ${routeData.longName ? `<span class="route-long-name">${routeData.longName}</span>` : ''}
                    </div>
                    <div class="route-controls">
                        <button class="btn-icon" onclick="toggleRouteVisibility(${routeId})" title="${routeData.visible ? 'Hide' : 'Show'} route">
                            ${visibilityIcon}
                        </button>
                        <button class="btn-icon btn-danger" onclick="stopTrackingRoute(${routeId})" title="Stop tracking">
                            ❌
                        </button>
                    </div>
                </div>
                <div class="route-status">
                    <span class="vehicle-count">${statusText}</span>
                    ${allVehiclesMode ? '<span class="mode-badge">All Vehicles Mode</span>' : ''}
                </div>
            </div>
        `;
  }).join('');
}

// Event listeners
function setupEventListeners() {
  // Track route button
  document.getElementById('trackRouteBtn').addEventListener('click', async () => {
    const routeId = document.getElementById('routeDropdown').value;
    if (routeId) {
      const trackBtn = document.getElementById('trackRouteBtn');
      const originalText = trackBtn.textContent;

      // Show loading state
      trackBtn.disabled = true;
      trackBtn.textContent = 'Loading...';

      try {
        await trackRoute(routeId);
      } finally {
        // Restore button state
        trackBtn.disabled = false;
        trackBtn.textContent = originalText;
      }
    } else {
      showStatus('Please select a route first', 'warning');
    }
  });

  // Show all vehicles button
  document.getElementById('showAllVehiclesBtn').addEventListener('click', showAllVehicles);

  // Clear all button
  document.getElementById('clearAllBtn').addEventListener('click', () => {
    trackedRoutes.forEach((routeData, routeId) => {
      if (routeData.interval) {
        clearInterval(routeData.interval);
      }
      routeData.markers.forEach(marker => map.removeLayer(marker));
    });

    trackedRoutes.clear();
    allVehiclesMode = false;
    updateShowAllVehiclesButton();
    updateTrackedRoutesList();

    showStatus('All routes cleared', 'info');
  });

  // Center map button
  document.getElementById('centerMapBtn').addEventListener('click', () => {
    map.setView([45.8150, 15.9819], 12);
  });

  // Dropdown enter key support
  document.getElementById('routeDropdown').addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
      document.getElementById('trackRouteBtn').click();
    }
  });
}

// Make functions globally available for inline event handlers
window.stopTrackingRoute = stopTrackingRoute;
window.toggleRouteVisibility = toggleRouteVisibility;

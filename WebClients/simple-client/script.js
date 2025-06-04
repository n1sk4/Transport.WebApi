// Configuration
const apiBaseUrl = '/api/GtfsData/GetAllVehiclePositionsByRouteId';
const routesApiUrl = '/api/GtfsData/GetAllStaticFileData?fileName=RoutesFile';

// Global variables
let trackedRoutes = new Map(); // Map of routeId -> {markers: [], interval: intervalId, vehicleCount: 0, visible: true, name: ''}
let availableRoutes = new Map(); // Map of routeId -> {id, name, type}
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

// Parse CSV-like route data
function parseRouteData(csvString) {
  const parts = [];
  let current = '';
  let inQuotes = false;
  
  for (let i = 0; i < csvString.length; i++) {
    const char = csvString[i];
    
    if (char === '"') {
      inQuotes = !inQuotes;
    } else if (char === ',' && !inQuotes) {
      parts.push(current.trim());
      current = '';
    } else {
      current += char;
    }
  }
  parts.push(current.trim());
  
  return {
    id: parts[2] ? parts[2].replace(/"/g, '') : '',
    name: parts[3] ? parts[3].replace(/"/g, '') : '',
    type: parts[5] ? parseInt(parts[5]) : null // 0 = tram, 3 = bus
  };
}

// Fetch and populate available routes
async function loadAvailableRoutes() {
  try {
    showStatus('Loading available routes...', 'loading');
    const response = await fetch(routesApiUrl);
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const routesData = await response.json();
    availableRoutes.clear();
    
    // Clear the select options
    routeSelect.innerHTML = '<option value="">Select a route...</option>';
    
    // Parse and add routes
    routesData.forEach(routeString => {
      const route = parseRouteData(routeString);
      if (route.id && route.name) {
        const routeId = parseInt(route.id);
        if (!isNaN(routeId)) {
          availableRoutes.set(routeId, route);
          
          const option = document.createElement('option');
          option.value = routeId;
          
          // Add emoji based on type
          const typeEmoji = route.type === 0 ? '🚋' : route.type === 3 ? '🚌' : '🚐';
          option.textContent = `${typeEmoji} ${route.id} - ${route.name}`;
          option.title = `${route.name} (${route.type === 0 ? 'Tram' : route.type === 3 ? 'Bus' : 'Other'})`; // Tooltip on hover
          routeSelect.appendChild(option);
        }
      }
    });
    
    // Enable the add button
    addButton.disabled = false;
    showStatus(`Loaded ${availableRoutes.size} routes`, 'success');
    
  } catch (error) {
    console.error('Error loading routes:', error);
    showStatus('Failed to load routes. Check console for details.', 'error');
    
    // Fallback - enable manual input
    routeSelect.innerHTML = '<option value="">Failed to load - enter manually</option>';
    addButton.disabled = false;
  }
}

// Create custom emoji marker for vehicles
function createVehicleMarker(lat, lng, routeId, vehicleIndex) {
  const color = getRouteColor(routeId);
  const routeInfo = availableRoutes.get(routeId);
  
  // Determine emoji based on route type
  let emoji = '🚐'; // default
  if (routeInfo) {
    emoji = routeInfo.type === 0 ? '🚋' : routeInfo.type === 3 ? '🚌' : '🚐';
  }
  
  const routeText = routeId.toString().slice(0, 3); // Limit to 3 digits for display
  
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
  });
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
    
    // Add tooltip with full route name and type
    const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Other';
    if (routeData.name) {
      routeItem.title = `${routeId} - ${routeData.name} (${vehicleType})`;
    }
    
    // Add click handler for visibility toggle
    routeItem.addEventListener('click', (e) => {
      // Don't toggle if clicking the remove button
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
    // Start tracking this route
    startTrackingRoute(routeId);
  } else {
    // Stop tracking and hide markers
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
  
  // Get route name from available routes
  const routeInfo = availableRoutes.get(routeId);
  const routeName = routeInfo ? routeInfo.name : '';
  const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Other';
  
  trackedRoutes.set(routeId, {
    markers: [],
    interval: null,
    vehicleCount: 0,
    visible: true,
    name: routeName
  });
  
  // Automatically start tracking when added
  startTrackingRoute(routeId);
  
  updateRoutesList();
  showStatus(`${vehicleType} ${routeId}${routeName ? ` (${routeName})` : ''} added and tracking started`, 'success');
  
  // Reset select
  routeSelect.value = '';
}

// Remove a route
function removeRoute(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData) return;
  
  // Stop tracking if active
  if (routeData.interval) {
    clearInterval(routeData.interval);
  }
  
  // Remove markers
  routeData.markers.forEach(marker => map.removeLayer(marker));
  
  // Remove from tracked routes
  trackedRoutes.delete(routeId);
  
  updateRoutesList();
  showStatus(`Route ${routeId} removed`, 'success');
}

// Make removeRoute available globally for onclick handlers
window.removeRoute = removeRoute;

// Function to fetch and plot vehicle positions for a specific route
async function fetchAndRenderVehicles(routeId) {
  const routeData = trackedRoutes.get(routeId);
  if (!routeData || !routeData.visible) return;
  
  try {
    const apiUrl = `${apiBaseUrl}?routeId=${routeId}`;
    const response = await fetch(apiUrl);
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    const vehicles = await response.json();
    
    // Clear existing markers for this route
    routeData.markers.forEach(marker => map.removeLayer(marker));
    routeData.markers = [];
    
    // Check if we got any vehicles
    if (!vehicles || vehicles.length === 0) {
      routeData.vehicleCount = 0;
      updateRoutesList();
      return;
    }
    
    let plotted = 0;
    
    // Plot new markers
    vehicles.forEach((vehicle, index) => {
      if (vehicle.hasLatitude && vehicle.hasLongitude) {
        const routeInfo = availableRoutes.get(routeId);
        const vehicleType = routeInfo && routeInfo.type === 0 ? 'Tram' : routeInfo && routeInfo.type === 3 ? 'Bus' : 'Vehicle';
        
        const marker = createVehicleMarker(vehicle.latitude, vehicle.longitude, routeId, index)
          .addTo(map)
          .bindPopup(`
            <strong>Route ${routeId}${routeData.name ? ` - ${routeData.name}` : ''}</strong><br>
            <strong>${vehicleType} ${index + 1}</strong><br>
            Latitude: ${vehicle.latitude.toFixed(6)}<br>
            Longitude: ${vehicle.longitude.toFixed(6)}
            ${vehicle.speed ? `<br>Speed: ${vehicle.speed} km/h` : ''}
            ${vehicle.bearing ? `<br>Bearing: ${vehicle.bearing}°` : ''}
          `);
        
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
  
  // Initial load
  fetchAndRenderVehicles(routeId);
  
  // Set up refresh interval
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
  
  // Clear markers
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

// Allow Enter key to add route when select is focused
routeSelect.addEventListener('keypress', (e) => {
  if (e.key === 'Enter' && routeSelect.value) {
    addButton.click();
  }
});

// Initialize the application
async function initialize() {
  // Load available routes first
  await loadAvailableRoutes();
  
  // Initialize with route 206 if it exists in available routes
  if (availableRoutes.has(206)) {
    routeSelect.value = 206;
    addRoute(206);
  }
}

// Start the application
initialize();

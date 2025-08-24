/**
 * Chart Preferences Manager
 * Handles saving and loading chart configuration preferences using localStorage
 * Usage: Include this file on any page with charts to maintain consistent preferences
 */

window.ChartPreferences = {
  // Default values
  defaults: {
    scalingMethod: 'raw',
    edgeStyle: 'light',
    themeMode: 'light'
  },
  
  // Save preferences to localStorage
  save: function(scalingMethod, edgeStyle, themeMode) {
    const prefs = {
      scalingMethod: scalingMethod || this.defaults.scalingMethod,
      edgeStyle: edgeStyle || this.defaults.edgeStyle,
      themeMode: themeMode || this.defaults.themeMode,
      timestamp: Date.now()
    };
    localStorage.setItem('chartPreferences', JSON.stringify(prefs));
    console.log('Chart preferences saved:', prefs);
  },
  
  // Load preferences from localStorage
  load: function() {
    try {
      const stored = localStorage.getItem('chartPreferences');
      if (stored) {
        const prefs = JSON.parse(stored);
        console.log('Chart preferences loaded:', prefs);
        return {
          scalingMethod: prefs.scalingMethod || this.defaults.scalingMethod,
          edgeStyle: prefs.edgeStyle || this.defaults.edgeStyle,
          themeMode: prefs.themeMode || this.defaults.themeMode
        };
      }
    } catch (error) {
      console.warn('Error loading chart preferences:', error);
    }
    return this.defaults;
  },
  
  // Apply preferences to dropdowns
  applyToDropdowns: function(scalingSelect, edgeSelect, themeSelect) {
    const prefs = this.load();
    if (scalingSelect && scalingSelect.value !== prefs.scalingMethod) {
      scalingSelect.value = prefs.scalingMethod;
    }
    if (edgeSelect && edgeSelect.value !== prefs.edgeStyle) {
      edgeSelect.value = prefs.edgeStyle;
    }
    if (themeSelect && themeSelect.value !== prefs.themeMode) {
      themeSelect.value = prefs.themeMode;
    }
    return prefs;
  },
  
  // Create standard dropdown HTML (for consistent UI across pages)
  createDropdownHTML: function(containerId = 'chart-controls') {
    return `
      <div id="${containerId}" style="position: absolute; top: 10px; right: 10px; z-index: 1000;">
        <select id="theme-mode" class="controlDD">
          <option value="light" selected>Light Mode</option>
          <option value="dark">Dark Mode</option>
        </select><br>
        <select id="scaling-method" class="controlDD">
          <option value="raw" selected>No Scaling</option>
          <option value="square-root">Square Root</option>
          <option value="proportional">Proportional</option>
          <option value="logarithmic">Logarithmic</option>
        </select><br>
        <select id="edge-style" class="controlDD">
          <option value="none">No Edge</option>
          <option value="light" selected>Light Edge</option>
          <option value="dark">Dark Edge</option>
        </select>
      </div>
    `;
  },
  
  // Add dark mode CSS styles (call this once per page)
  addDarkModeCSS: function() {
    const styleId = 'chart-preferences-dark-mode';
    if (document.getElementById(styleId)) return; // Already added
    
    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = `
      /* Chart preferences dark mode styling */
      .dark .title {
        fill: white;
        font-weight: bold;
      }
      .dark .titleLine {
        stroke: #666;
      }
      .dark text.titles {
        fill: white;
      }
      .dark #scaling-method,
      .dark #edge-style {
        background: #333;
        color: white;
        border-color: #555;
      }
      .dark #scaling-method option,
      .dark #edge-style option {
        background: #333;
        color: white;
      }
      
      /* Dark mode for matrix table */
      .dark #chart2-debug {
        color: white;
      }
      .dark #chart2-debug table {
        border-color: #555;
      }
      .dark #chart2-debug th,
      .dark #chart2-debug td {
        border-color: #555;
        color: white;
      }
    `;
    document.head.appendChild(style);
  },
  
  // Setup event listeners for dropdowns (call this after creating dropdowns)
  setupEventListeners: function(updateCallback) {
    const scalingSelect = document.getElementById('scaling-method');
    const edgeSelect = document.getElementById('edge-style');
    const themeSelect = document.getElementById('theme-mode');
    
    // Load and apply saved preferences on page load
    const savedPrefs = this.applyToDropdowns(scalingSelect, edgeSelect, themeSelect);
    console.log('Applied saved chart preferences:', savedPrefs);
    
    // Apply theme mode on page load if setSitelook function exists
    if (typeof setSitelook === 'function' && savedPrefs.themeMode) {
      setSitelook(savedPrefs.themeMode);
    }
    
    const self = this;
    function handleChange() {
      const selectedScaling = scalingSelect ? scalingSelect.value : savedPrefs.scalingMethod;
      const selectedEdge = edgeSelect ? edgeSelect.value : savedPrefs.edgeStyle;
      const selectedTheme = themeSelect ? themeSelect.value : savedPrefs.themeMode;
      
      // Save preferences whenever they change
      self.save(selectedScaling, selectedEdge, selectedTheme);
      
      // Call the provided update callback with current preferences
      if (updateCallback && typeof updateCallback === 'function') {
        updateCallback({
          scalingMethod: selectedScaling,
          edgeStyle: selectedEdge,
          themeMode: selectedTheme
        });
      }
    }
    
    function handleThemeChange() {
      const selectedTheme = themeSelect ? themeSelect.value : savedPrefs.themeMode;
      console.log(`Theme changed to: ${selectedTheme}`);
      
      // Apply theme change using setSitelook function if available
      if (typeof setSitelook === 'function') {
        setSitelook(selectedTheme);
      }
      
      // Call general change handler
      handleChange();
    }
    
    if (scalingSelect) {
      scalingSelect.addEventListener('change', handleChange);
    }
    
    if (edgeSelect) {
      edgeSelect.addEventListener('change', handleChange);
    }
    
    if (themeSelect) {
      themeSelect.addEventListener('change', handleThemeChange);
    }
    
    return savedPrefs;
  }
};

console.log('ChartPreferences module loaded');
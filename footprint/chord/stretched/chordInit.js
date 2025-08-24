import ChordDiagram from './script.js';

// Default configuration
const defaultConfig = {
    containerId: '#chart',
    titles: {
        left: "Sectors",
        right: "Indicators"
    }
};

// Initialize with sample data if no data is provided
let chordDiagram;

// Function to process sector supply data into chord diagram format
function processSectorData(sectorData, indicators, scalingMethod = 'raw') {
    // Limit to 10 indicators
    const limitedIndicators = indicators.slice(0, 10);
    
    // Debug: Check incoming sector data and identify the problem
    console.log(`ChordInit - Received ${sectorData.length} sectors:`);
    
    // Check the 10th sector (index 9) specifically
    if (sectorData.length >= 10) {
        const sector10 = sectorData[9];
        console.log(`DEBUGGING 10th sector: ${sector10.sector}`);
        
        // Check its values compared to others
        const indicatorSample = limitedIndicators[0]; // First indicator as test
        console.log(`Sample values for ${indicatorSample.code}:`);
        sectorData.slice(0, 10).forEach((sector, i) => {
            const value = sector[indicatorSample.code];
            console.log(`  Sector ${i}: ${sector.sector} = ${value ? value.toFixed(6) : 'undefined'}`);
        });
    }
    sectorData.forEach((s, i) => {
        console.log(`  Sector ${i}: ${s.sector}`);
    });
    
    // Create nodes array - sectors first, then indicators for proper positioning
    const nodes = [
        // Add sectors first (will be on left side)
        ...sectorData.map(s => ({
            id: s.sector,
            name: s.sector,
            type: 'sector',
            group: 1,
            metadata: s.metadata || {} // Include metadata for popups
        })),
        // Add indicators second (will be on right side)  
        ...limitedIndicators.map(i => ({
            id: i.code,
            name: i.name || i.code,
            type: 'indicator',
            group: 2,
            metadata: {
                code: i.code,
                fullName: i.name,
                unit: i.unit || 'N/A',
                category: i.category || 'Environmental'
            }
        }))
    ];
    
    console.log(`Nodes: ${nodes.length} total - ${sectorData.length} sectors, ${limitedIndicators.length} indicators`);

    // Calculate the raw matrix and find max values for normalization
    const rawMatrix = [];
    const numSectors = sectorData.length;
    const numIndicators = limitedIndicators.length;
    const totalNodes = numSectors + numIndicators;
    
    // Initialize matrix with zeros
    for (let i = 0; i < totalNodes; i++) {
        rawMatrix[i] = new Array(totalNodes).fill(0);
    }
    
    // Fill in the raw values (sectors to indicators only)
    sectorData.forEach((sector, sectorIndex) => {
        limitedIndicators.forEach((indicator, indicatorIndex) => {
            const value = sector[indicator.code];
            if (value !== undefined) {
                const targetIndex = numSectors + indicatorIndex;
                rawMatrix[sectorIndex][targetIndex] = Math.abs(value);
            }
        });
    });
    
    // Calculate current totals
    const sectorTotals = [];
    const indicatorTotals = [];
    
    for (let i = 0; i < numSectors; i++) {
        sectorTotals[i] = rawMatrix[i].reduce((sum, val) => sum + val, 0);
    }
    
    for (let i = 0; i < numIndicators; i++) {
        const indicatorIndex = numSectors + i;
        indicatorTotals[i] = 0;
        for (let j = 0; j < totalNodes; j++) {
            indicatorTotals[i] += rawMatrix[j][indicatorIndex];
        }
    }
    
    // Target: each side should be about 30% of circle (like chart1)
    // Scale values so the largest sector/indicator arc is reasonable
    const maxSectorTotal = Math.max(...sectorTotals);
    const maxIndicatorTotal = Math.max(...indicatorTotals);
    
    // Scale factor to make largest arc about 30% max
    const targetMaxValue = 1000; // Arbitrary target for manageable arc sizes
    const sectorScale = targetMaxValue / Math.max(maxSectorTotal, 1);
    const indicatorScale = targetMaxValue / Math.max(maxIndicatorTotal, 1);
    
    // Reduced logging
    console.log(`Max sector total: ${maxSectorTotal.toFixed(2)}, Max indicator total: ${maxIndicatorTotal.toFixed(2)}`);
    
    // Normalize data to prevent extreme outliers from dominating
    // Find all values first and analyze by sector
    const allValues = [];
    const sectorMaxValues = [];
    
    sectorData.forEach((sector, sectorIndex) => {
        let sectorMax = 0;
        limitedIndicators.forEach((indicator, indicatorIndex) => {
            const value = sector[indicator.code];
            if (value !== undefined && value !== 0) {
                const absValue = Math.abs(value);
                allValues.push(absValue);
                sectorMax = Math.max(sectorMax, absValue);
            }
        });
        sectorMaxValues.push(sectorMax);
        // Only log problematic sectors
        if (sectorIndex === 9 || sectorMax > 1000) {
            console.log(`Sector ${sectorIndex} (${sector.sector}) max value: ${sectorMax.toFixed(6)}`);
        }
    });
    
    // Use a more balanced capping approach
    allValues.sort((a, b) => a - b);
    const q3 = allValues[Math.floor(allValues.length * 0.75)];
    const q99 = allValues[Math.floor(allValues.length * 0.99)]; // 99th percentile instead of Q3*2
    
    // Also check sector balance 
    sectorMaxValues.sort((a, b) => a - b);
    const medianSectorMax = sectorMaxValues[Math.floor(sectorMaxValues.length / 2)];
    const sectorCap = medianSectorMax * 20; // Allow more variation (20x instead of 5x)
    
    // Use the larger of the two to preserve meaningful differences
    const finalCap = Math.max(q99, sectorCap); // Use MAX instead of MIN to be less aggressive
    
    console.log(`Final cap applied: ${finalCap.toFixed(6)} (from Q99: ${q99.toFixed(6)}, sector balance: ${sectorCap.toFixed(6)})`);

    // Apply chosen scaling method
    const links = [];
    const allNonZeroValues = [];
    
    // First pass: collect all non-zero values for scaling reference
    sectorData.forEach((sector, sectorIndex) => {
        limitedIndicators.forEach((indicator, indicatorIndex) => {
            const originalValue = sector[indicator.code];
            if (originalValue !== undefined && originalValue !== 0) {
                allNonZeroValues.push(Math.abs(originalValue));
            }
        });
    });
    
    allNonZeroValues.sort((a, b) => a - b);
    const minValue = allNonZeroValues[0];
    const maxValue = allNonZeroValues[allNonZeroValues.length - 1];
    
    console.log(`Scaling method: ${scalingMethod}, min=${minValue.toFixed(6)}, max=${maxValue.toFixed(6)}`);
    
    // Function to apply different scaling methods
    function scaleValue(value, method) {
        const absValue = Math.abs(value);
        
        switch (method) {
            case 'logarithmic': {
                const logMin = Math.log10(minValue);
                const logMax = Math.log10(maxValue);
                const logRange = logMax - logMin;
                const logValue = Math.log10(absValue);
                const normalizedLog = (logValue - logMin) / logRange;
                return normalizedLog * 0.1 + 0.001; // Scale to 0.001-0.101 range
            }
            
            case 'proportional': {
                // Simple min-max normalization
                const normalized = (absValue - minValue) / (maxValue - minValue);
                return normalized * 0.1 + 0.001; // Scale to 0.001-0.101 range
            }
            
            case 'square-root': {
                // Square root scaling reduces extreme differences less aggressively than log
                const sqrtValue = Math.sqrt(absValue);
                const sqrtMin = Math.sqrt(minValue);
                const sqrtMax = Math.sqrt(maxValue);
                const normalized = (sqrtValue - sqrtMin) / (sqrtMax - sqrtMin);
                return normalized * 0.1 + 0.001;
            }
            
            case 'raw':
            default: {
                // Use raw values with minimal cap
                return Math.min(absValue, finalCap);
            }
        }
    }
    
    // Second pass: create links with chosen scaling
    sectorData.forEach((sector, sectorIndex) => {
        limitedIndicators.forEach((indicator, indicatorIndex) => {
            const originalValue = sector[indicator.code];
            if (originalValue !== undefined && originalValue !== 0) {
                const scaledValue = scaleValue(originalValue, scalingMethod);
                
                links.push({
                    source: sector.sector,
                    target: indicator.code,
                    value: scaledValue,
                    originalValue: originalValue // Store original for tooltips
                });
            }
        });
    });

    return { nodes, links };
}

// Initialize chord diagram with sector supply data
export async function initializeChordDiagram(sectorData, indicators, config = {}) {
    try {
        const combinedConfig = { ...defaultConfig, ...config };
        const scalingMethod = config.scalingMethod || 'raw';
        const chordData = processSectorData(sectorData, indicators, scalingMethod);
        
        // Destroy existing diagram if it exists
        if (chordDiagram) {
            d3.select(combinedConfig.containerId).html('');
        }

        // Create new chord diagram
        chordDiagram = new ChordDiagram(combinedConfig.containerId, chordData, combinedConfig);
        
        return chordDiagram;
    } catch (error) {
        console.error('Error initializing chord diagram:', error);
        throw error;
    }
}

// Event listener for data updates
document.addEventListener('dataUpdated', (event) => {
    if (event.detail && event.detail.sectorData && event.detail.indicators) {
        initializeChordDiagram(event.detail.sectorData, event.detail.indicators);
    }
});

// Export diagram instance for external access
export { chordDiagram };
function drawDynamicLineAndDisplayDistance(UIAxes, DPI)
    % Store ROI handles and text objects in a persistent variable
    persistent lines distances;

    if isempty(lines)
        lines = {}; % Initialize an empty cell array to store ROI handles
        distances = {}; % Initialize an empty cell array to store text objects
    end

    % Create a new line ROI using drawline
    h = drawline('Color', 'r', 'LineWidth', 1, 'Parent', UIAxes);
    
    % Initial update of the distance when the line is first created
    updateDistance(h, []);

    % Add a listener for changes in the ROI (when the line is moved)
    addlistener(h, 'MovingROI', @(src, evt) updateDistance(src, distances{end}));

    % Nested function to calculate and display the distance
    function updateDistance(roi, distText)
        % Get the current position of the ROI
        pos = roi.Position;
        point1 = pos(1, :);
        point2 = pos(2, :);
        distance = sqrt(sum((point1 - point2).^2)) * 25.4 / DPI; % Distance in mm
        
        % Calculate the midpoint to place the distance text
        midPoint = (point1 + point2) / 2;

        % If a text object already exists, update it; otherwise, create a new one
        if isempty(distText) || ~isvalid(distText)
            distText = text(UIAxes, midPoint(1), midPoint(2), sprintf('Distance: %.2f mm', distance), ...
                        'Color', 'yellow', 'FontSize', 12, 'HorizontalAlignment', 'center');
            distances{end+1} = distText; % Store the text handle in the persistent variable
        else
            % Update the existing text position and content
            distText.Position = midPoint;
            distText.String = sprintf('Distance: %.2f mm', distance);
        end
    end
end

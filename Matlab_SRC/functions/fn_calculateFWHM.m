function [fwhm, profileData] = fn_calculateFWHM(matrix, direction, expandPercentage, peakMethod, figureAxes)
    % Function to calculate and plot Full Width Half Maximum (FWHM)
    % for a dose profile in either the X or Y direction.
    %
    % Parameters:
    % matrix          - Input matrix with dose values (numeric matrix)
    % direction       - Profile direction ('X' or 'Y')
    % expandPercentage- Percentage to expand profile around center (0-100)
    % peakMethod      - Method to determine peak ('mean', 'max', 'mode')
    % figureAxes      - Handle to axes for plotting
    %
    % Returns:
    % fwhm           - Calculated Full Width Half Maximum value
    % profileData    - Structure containing profile information for further analysis
    
    % Input validation
    validateInputs();
    
    % Extract profile data
    [dis_profile, profile] = extractProfile();
    
    % Find center using distance information
    center_idx = findProfileCenter();
    
    % Process profile and calculate FWHM
    [expanded_profile, expanded_dis_profile] = expandProfile();
    profile_peak = calculatePeak();
    [fwhm, pos_left, pos_right] = calculateFWHM();
    
    % Generate plot
    if ~isempty(figureAxes) && isvalid(figureAxes)
        generatePlot();
    end
    
    % Package output data
    profileData = packageOutputData();
    
    %% Nested functions for better organization and scope management
    
    function validateInputs()
        % Validate matrix
        validateattributes(matrix, {'numeric'}, {'2d', 'nonempty', 'finite'}, 'fn_calculateFWHM', 'matrix');
        
        % Validate direction
        if ~ischar(direction) || ~ismember(lower(direction), {'x', 'y'})
            error('Direction must be either ''X'' or ''Y''.');
        end
        
        % Validate expandPercentage
        validateattributes(expandPercentage, {'numeric'}, {'scalar', '>=', 0, '<=', 100}, 'fn_calculateFWHM', 'expandPercentage');
        
        % Validate peakMethod
        if ~ischar(peakMethod) || ~ismember(lower(peakMethod), {'mean', 'max', 'maximum', 'mode'})
            error('PeakMethod must be ''mean'', ''max'', or ''mode''.');
        end
        
        % Validate figureAxes
        if ~isempty(figureAxes) && (~ishandle(figureAxes) || ~strcmp(get(figureAxes, 'Type'), 'axes'))
            error('FigureAxes must be a valid axes handle or empty.');
        end
        
        % Validate matrix structure
        if size(matrix, 1) < 2 || size(matrix, 2) < 2
            error('Matrix must have at least 2 rows and 2 columns.');
        end
    end
    
    function [dis_profile, profile] = extractProfile()
        try
            if strcmpi(direction, 'X')
                dis_profile = matrix(2:end, 1);  % Y values for X profile
                valid_cols = matrix(1, 2:end) == 0;
                if ~any(valid_cols)
                    error('No valid profile data found for X direction.');
                end
                profile = matrix(2:end, valid_cols);  % Profile data column
            else % Y direction
                dis_profile = matrix(1, 2:end)';  % X values
                valid_rows = matrix(2:end, 1) == 0;
                if ~any(valid_rows)
                    error('No valid profile data found for Y direction.');
                end
                profile = matrix(valid_rows, 2:end);  % Profile data row
                profile = profile(:);
            end
            
            % Validate extracted data
            if any(isnan(dis_profile)) || any(isnan(profile))
                error('Profile contains NaN values.');
            end
        catch ME
            error('Failed to extract profile: %s', ME.message);
        end
    end
    
    function center_idx = findProfileCenter()
        try
            % Find the index closest to zero in the distance profile
            [~, center_idx] = min(abs(dis_profile));
            
            if isempty(center_idx)
                error('Could not determine profile center.');
            end
            
            % Validate that we found a reasonable center
            if abs(dis_profile(center_idx)) > max(abs(dis_profile))/10
                warning('Center point is far from zero. Check distance calibration.');
            end
            
        catch ME
            error('Failed to find profile center: %s', ME.message);
        end
    end
    
    function [expanded_profile, expanded_dis_profile] = expandProfile()
        try
            % Calculate expansion range based on percentage
            half_range = round(length(profile) * (expandPercentage / 100) / 2);
            
            % Ensure minimum range of 1
            half_range = max(1, half_range);
            
            % Calculate expansion indices around true center
            start_idx = max(1, center_idx - half_range);
            end_idx = min(length(profile), center_idx + half_range);
            
            expanded_profile = profile(start_idx:end_idx);
            expanded_dis_profile = dis_profile(start_idx:end_idx);
            
            if isempty(expanded_profile)
                error('Expanded profile is empty.');
            end
            
            % Validate expansion
            if length(expanded_profile) < 3
                warning('Very small expansion range. Consider increasing expandPercentage.');
            end
            
        catch ME
            error('Failed to expand profile: %s', ME.message);
        end
    end
    
    function peak = calculatePeak()
        try
            switch lower(peakMethod)
                case {'maximum', 'max'}
                    peak = max(expanded_profile);
                case 'mean'
                    peak = mean(expanded_profile);
                case 'mode'
                    peak = mode(expanded_profile);
                otherwise
                    error('Invalid peak method.');
            end
            
            if peak <= 0
                error('Invalid peak value: peak must be positive.');
            end
        catch ME
            error('Failed to calculate peak: %s', ME.message);
        end
    end
    
    function [fwhm_val, left_pos, right_pos] = calculateFWHM()
        try
            half_max = profile_peak / 2;
            
            % Find left intersection (searching from center to left)
            left_profile = profile(1:center_idx);
            left_diff = abs(left_profile - half_max);
            [~, left_idx] = min(left_diff);
            
            % Find right intersection (searching from center to right)
            right_profile = profile(center_idx:end);
            right_diff = abs(right_profile - half_max);
            [~, temp_right_idx] = min(right_diff);
            right_idx = temp_right_idx + center_idx - 1;
            
            % Get positions
            left_pos = dis_profile(left_idx);
            right_pos = dis_profile(right_idx);
            
            % Calculate FWHM
            fwhm_val = abs(right_pos - left_pos);
            
            if fwhm_val <= 0
                error('Invalid FWHM calculation: value must be positive.');
            end
            
            % Validate FWHM measurement
            if abs(profile(left_idx) - half_max)/half_max > 0.1 || ...
               abs(profile(right_idx) - half_max)/half_max > 0.1
                warning('Large deviation in FWHM intersection points. Results may be inaccurate.');
            end
            
        catch ME
            error('Failed to calculate FWHM: %s', ME.message);
        end
    end
    
    function generatePlot()
        try
            % Create main plot
            plot(figureAxes, dis_profile, profile, '-b', 'LineWidth', 1.5, 'DisplayName', 'Dose Profile');
            hold(figureAxes, 'on');
            
            % Plot expanded segment
            plot(figureAxes, expanded_dis_profile, expanded_profile, '-g', 'LineWidth', 1.5, 'DisplayName', 'Expanded Profile');
            
            % Add FWHM indicators
            yline(figureAxes, profile_peak/2, '--r', 'LineWidth', 1.2, 'DisplayName', 'Half Max');
            xline(figureAxes, pos_left, '--k', 'LineWidth', 1.2, 'DisplayName', 'Left FWHM');
            xline(figureAxes, pos_right, '--k', 'LineWidth', 1.2, 'DisplayName', 'Right FWHM');
            
            % Mark center point
            plot(figureAxes, dis_profile(center_idx), profile(center_idx), 'ro', ...
                'MarkerSize', 8, 'DisplayName', 'Center');
            
            % Add labels and formatting
            xlabel(figureAxes, ['Distance (' direction ')']);
            ylabel(figureAxes, 'Dose Profile');
            title(figureAxes, sprintf('FWHM: %.2f | Center at %.2f', fwhm, dis_profile(center_idx)));
            legend(figureAxes, 'Location', 'Best');
            grid(figureAxes, 'on');
            hold(figureAxes, 'off');
        catch ME
            warning('Failed to generate plot: %s', ME.message);
        end
    end
    
    function data = packageOutputData()
        data = struct(...
            'profile', profile, ...
            'distance', dis_profile, ...
            'expandedProfile', expanded_profile, ...
            'expandedDistance', expanded_dis_profile, ...
            'centerIndex', center_idx, ...
            'centerPosition', dis_profile(center_idx), ...
            'peakValue', profile_peak, ...
            'halfMax', profile_peak/2, ...
            'leftPosition', pos_left, ...
            'rightPosition', pos_right, ...
            'fwhm', fwhm ...
        );
    end
end
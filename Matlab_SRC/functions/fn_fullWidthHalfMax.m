function [dis_x, dis_y, std_x, std_y] = fn_fullWidthHalfMax(final_dose, plateau_x, plateau_y, method, UIAxes1, UIAxes2)
    % fn_fullWidthHalfMax  Multi-line FWHM jaw size estimator natively calculated in mm.
    %
    % Inputs:
    %   final_dose  - Augmented dose matrix: row 1 = X-mm coords, col 1 = Y-mm coords, (2:end,2:end) = dose values
    %   plateau_x   - Physical width (mm) of the central flat region in X direction
    %   plateau_y   - Physical width (mm) of the central flat region in Y direction
    %   method      - Peak estimation method: 'Maximum', 'Mean', or 'Median'
    %   UIAxes1     - Axes for Y-profile display
    %   UIAxes2     - Axes for X-profile display
    %
    % Outputs:
    %   dis_x - Mean FWHM in X direction (mm)
    %   dis_y - Mean FWHM in Y direction (mm)
    %   std_x - Standard deviation of FWHM across X scan lines (mm)
    %   std_y - Standard deviation of FWHM across Y scan lines (mm)

    % Find the absolute geometrical centre index (mm coordinate closest to 0)
    [~, X_center_rel] = min(abs(final_dose(2:end, 1)));
    [~, Y_center_rel] = min(abs(final_dose(1, 2:end)));
    row_center_idx = X_center_rel + 1;
    col_center_idx = Y_center_rel + 1;

    % X_dis = X-mm coordinates (from first row), Y_dis = Y-mm coordinates (from first column)
    X_dis = final_dose(1, 2:end);
    Y_dis = final_dose(2:end, 1);

    X_center_val = final_dose(1, col_center_idx);
    Y_center_val = final_dose(row_center_idx, 1);

    % Find all rows/columns that fall physically within the defined plateau bands.
    % We compute boundaries relative to the detected geometric center.
    % To support asymmetric fields perfectly, we sweep along Y using the Y-Plateau,
    % and we sweep along X using the X-Plateau.
    valid_rows_for_X = find(Y_dis >= Y_center_val - plateau_y/2 & Y_dis <= Y_center_val + plateau_y/2);
    valid_cols_for_Y = find(X_dis >= X_center_val - plateau_x/2 & X_dis <= X_center_val + plateau_x/2);
    
    nValidX = length(valid_rows_for_X);
    nValidY = length(valid_cols_for_Y);
    
    X_locations_left  = zeros(nValidX, 1);
    X_locations_right = zeros(nValidX, 1);
    Y_locations_left  = zeros(nValidY, 1);
    Y_locations_right = zeros(nValidY, 1);
    
    % Store all profiles for overlay visualisation
    X_profiles_all = zeros(nValidX, length(X_dis));
    Y_profiles_all = zeros(nValidY, length(Y_dis));
    
    h = waitbar(0, 'Mathematically interpolating FWHM beam edges natively in mm...', 'Name', 'FWHM');
    pause(0.05);

    try
        % ---- X PROFILES (horizontal row scans) ----
        validX = 0;
        for i = 1:nValidX
            row_idx = valid_rows_for_X(i);
            % Need to add 1 because final_dose(1,:) is coords, so actual dose starts row 2
            if row_idx + 1 > size(final_dose, 1)
                continue;
            end
            validX = validX + 1;
            X_profile = final_dose(row_idx + 1, 2:end);
            X_profile = X_profile(:)';
            X_profiles_all(validX, :) = X_profile;

            % --- Plateau region for peak estimation ---
            if plateau_x > 0
                valid_x_idx = find(X_dis >= X_center_val - plateau_x/2 & X_dis <= X_center_val + plateau_x/2);
                if ~isempty(valid_x_idx)
                    x_start = min(valid_x_idx);
                    x_end   = max(valid_x_idx);
                else
                    x_start = []; x_end = [];
                end
            else
                x_start = []; x_end = [];  % fallback below
            end
            % Guard: if plateau is empty or degenerate, use the full profile
            if isempty(x_start) || isempty(x_end) || x_start >= x_end
                x_start = 1; x_end = length(X_profile);
            end
            half_profile_x = X_profile(x_start:x_end);

            switch lower(method)
                case 'maximum', X_max_val = max(half_profile_x);
                case 'mean',    X_max_val = mean(half_profile_x);
                case 'median',  X_max_val = median(half_profile_x);
                otherwise,      X_max_val = mean(half_profile_x);
            end
            threshold_X = X_max_val / 2;

            % --- Left edge (ascending slope): sub-pixel interpolation ---
            [~, max_idx_X] = max(X_profile);
            idx_above_left_X = find(X_profile(1:max_idx_X) >= threshold_X, 1, 'first');
            if isempty(idx_above_left_X) || idx_above_left_X == 1
                X_locations_left(validX) = X_dis(1);
            else
                idx1 = idx_above_left_X - 1; idx2 = idx_above_left_X;
                dv = X_profile(idx2) - X_profile(idx1);
                fraction = (dv ~= 0) * (threshold_X - X_profile(idx1)) / max(dv, eps);
                X_locations_left(validX) = X_dis(idx1) + fraction * (X_dis(idx2) - X_dis(idx1));
            end

            % --- Right edge (descending slope): sub-pixel interpolation ---
            idx_below_right_X = find(X_profile(max_idx_X:end) < threshold_X, 1, 'first');
            if isempty(idx_below_right_X)
                X_locations_right(validX) = X_dis(end);
            else
                g2 = max_idx_X + idx_below_right_X - 1; g1 = g2 - 1;
                dv = X_profile(g2) - X_profile(g1);
                fraction = (dv ~= 0) * (threshold_X - X_profile(g1)) / min(dv, -eps);
                X_locations_right(validX) = X_dis(g1) + fraction * (X_dis(g2) - X_dis(g1));
            end

            waitbar(i / (nValidX + nValidY), h, 'Processing X-profiles...');
        end

        % ---- Y PROFILES (vertical column scans) ----
        validY = 0;
        for j = 1:nValidY
            col_idx = valid_cols_for_Y(j);
            % Need to add 1 because final_dose(:,1) is coords, so actual dose starts col 2
            if col_idx + 1 > size(final_dose, 2)
                continue;
            end
            validY = validY + 1;
            Y_profile = final_dose(2:end, col_idx + 1);
            Y_profile = Y_profile(:)';
            Y_profiles_all(validY, :) = Y_profile;

            % --- Plateau region for peak estimation ---
            if plateau_y > 0
                valid_y_idx = find(Y_dis >= Y_center_val - plateau_y/2 & Y_dis <= Y_center_val + plateau_y/2);
                if ~isempty(valid_y_idx)
                    y_start = min(valid_y_idx);
                    y_end   = max(valid_y_idx);
                else
                    y_start = []; y_end = [];
                end
            else
                y_start = []; y_end = [];
            end
            if isempty(y_start) || isempty(y_end) || y_start >= y_end
                y_start = 1; y_end = length(Y_profile);
            end
            half_profile_y = Y_profile(y_start:y_end);

            switch lower(method)
                case 'maximum', Y_max_val = max(half_profile_y);
                case 'mean',    Y_max_val = mean(half_profile_y);
                case 'median',  Y_max_val = median(half_profile_y);
                otherwise,      Y_max_val = mean(half_profile_y);
            end
            threshold_Y = Y_max_val / 2;

            % --- Left edge ---
            [~, max_idx_Y] = max(Y_profile);
            idx_above_left_Y = find(Y_profile(1:max_idx_Y) >= threshold_Y, 1, 'first');
            if isempty(idx_above_left_Y) || idx_above_left_Y == 1
                Y_locations_left(validY) = Y_dis(1);
            else
                idx1 = idx_above_left_Y - 1; idx2 = idx_above_left_Y;
                dv = Y_profile(idx2) - Y_profile(idx1);
                fraction = (dv ~= 0) * (threshold_Y - Y_profile(idx1)) / max(dv, eps);
                Y_locations_left(validY) = Y_dis(idx1) + fraction * (Y_dis(idx2) - Y_dis(idx1));
            end

            % --- Right edge ---
            idx_below_right_Y = find(Y_profile(max_idx_Y:end) < threshold_Y, 1, 'first');
            if isempty(idx_below_right_Y)
                Y_locations_right(validY) = Y_dis(end);
            else
                g2 = max_idx_Y + idx_below_right_Y - 1; g1 = g2 - 1;
                dv = Y_profile(g2) - Y_profile(g1);
                fraction = (dv ~= 0) * (threshold_Y - Y_profile(g1)) / min(dv, -eps);
                Y_locations_right(validY) = Y_dis(g1) + fraction * (Y_dis(g2) - Y_dis(g1));
            end

            waitbar((nValidX + j) / (nValidX + nValidY), h, 'Processing Y-profiles...');
        end

        % ---- RESULTS & VISUALISATION ----
        dis_x = 0; dis_y = 0; std_x = 0; std_y = 0;

        if validX > 0
            fwhms_x      = abs(X_locations_right(1:validX) - X_locations_left(1:validX));
            dis_x        = round(mean(fwhms_x), 3);
            std_x        = round(std(fwhms_x),  4);
            X_profile_mean = mean(X_profiles_all(1:validX, :), 1);
            avg_left_x   = mean(X_locations_left(1:validX));
            avg_right_x  = mean(X_locations_right(1:validX));
            threshold_vis_x = max(X_profile_mean) / 2;  % threshold of the average profile

            delete(allchild(UIAxes2)); hold(UIAxes2, 'on');
            yBounds = [min(X_profile_mean)*0.85, max(X_profile_mean)*1.1];
            % Shaded Plateau overlay (subtle blue), shifted to real X center
            patch_x_coords = [-plateau_x/2, plateau_x/2, plateau_x/2, -plateau_x/2] + X_center_val;
            patch(UIAxes2, patch_x_coords, ...
                 [yBounds(1), yBounds(1), yBounds(2), yBounds(2)], ...
                 [0.85 0.92 1.0], 'FaceAlpha', 0.4, 'EdgeColor', 'none', 'DisplayName', 'Peak Plateau');
            % Sample max 10 individual scan lines for visualization to prevent visual clutter
            if validX > 10
                vis_idx_x = round(linspace(1, validX, 10));
            else
                vis_idx_x = 1:validX;
            end
            plot(UIAxes2, X_dis, X_profiles_all(vis_idx_x, :)', ...
                'Color', [0.7 0.75 0.8], 'LineWidth', 0.4, 'HandleVisibility', 'off');
            % Mean profile (blue)
            plot(UIAxes2, X_dis, X_profile_mean, 'b-', 'LineWidth', 2, 'DisplayName', 'Mean Profile');
            % Horizontal 50% threshold dashed line
            xLims = [X_dis(1), X_dis(end)];
            plot(UIAxes2, xLims, [threshold_vis_x, threshold_vis_x], 'k:', 'LineWidth', 1, ...
                'DisplayName', '50% threshold');
            % Vertical edge markers
            yBounds = [min(X_profile_mean)*0.85, max(X_profile_mean)*1.1];
            plot(UIAxes2, [avg_left_x,  avg_left_x],  yBounds, 'r--', 'LineWidth', 1.2, 'HandleVisibility', 'off');
            plot(UIAxes2, [avg_right_x, avg_right_x], yBounds, 'g--', 'LineWidth', 1.2, 'HandleVisibility', 'off');
            % Intersection dots
            plot(UIAxes2, avg_left_x,  threshold_vis_x, 'ro', 'MarkerSize', 8, 'MarkerFaceColor', 'r');
            plot(UIAxes2, avg_right_x, threshold_vis_x, 'go', 'MarkerSize', 8, 'MarkerFaceColor', 'g');
            xlabel(UIAxes2, 'Distance X (mm)'); ylabel(UIAxes2, 'Dose');
            grid(UIAxes2, 'on'); legend(UIAxes2, 'show', 'Location', 'south');
            title(UIAxes2, sprintf('XX-Profile  |  FWHM = %.3f ± %.4f mm', dis_x, std_x));
            hold(UIAxes2, 'off');
        end

        if validY > 0
            fwhms_y      = abs(Y_locations_right(1:validY) - Y_locations_left(1:validY));
            dis_y        = round(mean(fwhms_y), 3);
            std_y        = round(std(fwhms_y),  4);
            Y_profile_mean = mean(Y_profiles_all(1:validY, :), 1);
            avg_left_y   = mean(Y_locations_left(1:validY));
            avg_right_y  = mean(Y_locations_right(1:validY));
            threshold_vis_y = max(Y_profile_mean) / 2;

            delete(allchild(UIAxes1)); hold(UIAxes1, 'on');
            yBoundsY = [min(Y_profile_mean)*0.85, max(Y_profile_mean)*1.1];
            % Shaded Plateau overlay (subtle green), shifted to real Y center
            patch_y_coords = [-plateau_y/2, plateau_y/2, plateau_y/2, -plateau_y/2] + Y_center_val;
            patch(UIAxes1, patch_y_coords, ...
                 [yBoundsY(1), yBoundsY(1), yBoundsY(2), yBoundsY(2)], ...
                 [0.85 0.98 0.88], 'FaceAlpha', 0.4, 'EdgeColor', 'none', 'DisplayName', 'Peak Plateau');
            % Sample max 10 individual scan lines for visualization to prevent visual clutter
            if validY > 10
                vis_idx_y = round(linspace(1, validY, 10));
            else
                vis_idx_y = 1:validY;
            end
            plot(UIAxes1, Y_dis, Y_profiles_all(vis_idx_y, :)', ...
                'Color', [0.7 0.75 0.8], 'LineWidth', 0.4, 'HandleVisibility', 'off');
            plot(UIAxes1, Y_dis, Y_profile_mean, 'b-', 'LineWidth', 2, 'DisplayName', 'Mean Profile');
            yLims = [Y_dis(1), Y_dis(end)];
            plot(UIAxes1, yLims, [threshold_vis_y, threshold_vis_y], 'k:', 'LineWidth', 1, ...
                'DisplayName', '50% threshold');
            yBounds = [min(Y_profile_mean)*0.85, max(Y_profile_mean)*1.1];
            plot(UIAxes1, [avg_left_y,  avg_left_y],  yBounds, 'r--', 'LineWidth', 1.2, 'HandleVisibility', 'off');
            plot(UIAxes1, [avg_right_y, avg_right_y], yBounds, 'g--', 'LineWidth', 1.2, 'HandleVisibility', 'off');
            plot(UIAxes1, avg_left_y,  threshold_vis_y, 'ro', 'MarkerSize', 8, 'MarkerFaceColor', 'r');
            plot(UIAxes1, avg_right_y, threshold_vis_y, 'go', 'MarkerSize', 8, 'MarkerFaceColor', 'g');
            xlabel(UIAxes1, 'Distance Y (mm)'); ylabel(UIAxes1, 'Dose');
            grid(UIAxes1, 'on'); legend(UIAxes1, 'show', 'Location', 'south');
            title(UIAxes1, sprintf('YY-Profile  |  FWHM = %.3f ± %.4f mm', dis_y, std_y));
            hold(UIAxes1, 'off');
        end
        
        drawnow;

    catch ME
        close(h);
        rethrow(ME);
    end
    close(h);
end

function [TPS_augmented, Film_augmented,...
    tpsX, tpsY, filmX, filmY] = fn_AugmentedPlaneDose(dicomplane, pixelspacing, DoseMatrix, DPI, ProfilePoint)
    % Size of TPS plane in pixels
    [t_row_px, t_col_px, ~] = size(dicomplane);
    % Size of Film plane in pixels
    [f_row_px, f_col_px, ~] = size(DoseMatrix);
    
    % Size of TPS plane in mm
    t_row_mm = t_row_px * pixelspacing(1);
    t_col_mm = t_col_px * pixelspacing(2);

    % Size of Film plane in mm
    f_row_mm = f_row_px * 25.4 / DPI; % convert pixels to mm using DPI
    f_col_mm = f_col_px * 25.4 / DPI;

    % Calculate center points
    tps_center_x = t_col_mm / 2;
    tps_center_y = t_row_mm / 2;
    film_center_x = f_col_mm / 2;
    film_center_y = f_row_mm / 2;
    
    % Create coordinate vectors
    tps_x = linspace(-tps_center_x, tps_center_x, t_col_px);
    tps_y = linspace(-tps_center_y, tps_center_y, t_row_px);
    film_x = linspace(-film_center_x, film_center_x, f_col_px);
    film_y = linspace(-film_center_y, film_center_y, f_row_px);
    
    % Create augmented matrices (increased by 1 row and column to store coordinates)
    TPS_augmented = zeros(t_row_px + 1, t_col_px + 1);
    TPS_augmented(2:end, 2:end) = dicomplane;   
    TPS_augmented(1, 2:end) = tps_x;      
    TPS_augmented(2:end, 1) = tps_y';    

    % Same process for the Film plane
    Film_augmented = zeros(f_row_px + 1, f_col_px + 1);
    Film_augmented(2:end, 2:end) = DoseMatrix;   
    Film_augmented(1, 2:end) = film_x;          
    Film_augmented(2:end, 1) = film_y';  


    if ProfilePoint ~= 0

        % Convert the ProfilePoint to physical coordinates in TPS space
        Point_X_TPS_mm = ( round(t_row_px/2) -ProfilePoint(1))* pixelspacing(1);  
        Point_Y_TPS_mm = (round(t_col_px/2) - ProfilePoint(2)) * pixelspacing(2); 

    else       

        % Convert the ProfilePoint to physical coordinates in TPS space
        Point_X_TPS_mm = (ProfilePoint(1))* pixelspacing(1);  
        Point_Y_TPS_mm = (ProfilePoint(2)) * pixelspacing(2);
        
    end

    % Find the closest index in both TPS and Film based on the physical mm position
    [~, t_x_idx] = min(abs(tps_x - Point_X_TPS_mm)); 
    [~, t_y_idx] = min(abs(tps_y - Point_Y_TPS_mm)); 

    [~, f_x_idx] = min(abs(film_x - Point_X_TPS_mm)); 
    [~, f_y_idx] = min(abs(film_y - Point_Y_TPS_mm)); 

    % Extract profiles based on calculated indices
    tps_x_profile = dicomplane(:, t_x_idx); 
    tps_y_profile = dicomplane(t_y_idx, :); 

    film_x_profile = DoseMatrix(f_y_idx, :); 
    film_y_profile = DoseMatrix(:, f_x_idx);

    % Output profiles combining coordinates and extracted profiles
    tpsX = [tps_x', tps_y_profile'];  
    tpsY = [tps_y', tps_x_profile];  
    
    filmX = [film_x', film_x_profile'];   
    filmY = [film_y', film_y_profile];   

end

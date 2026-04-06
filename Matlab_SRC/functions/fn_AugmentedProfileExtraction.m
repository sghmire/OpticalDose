function [tpsX, tpsY, filmX, filmY] = fn_AugmentedProfileExtraction(AugPlanDose, AugFilmDose, pixelspacing, ProfilePoint)

    % Extracting the physical distance row and column from Augmented Dose
    tps_x = AugPlanDose(2:end, 1); 
    tps_y = AugPlanDose(1, 2:end); 
    film_x = AugFilmDose(2:end, 1); 
    film_y = AugFilmDose(1, 2:end); 

    %Extracting the pixel values matrix only
    AugPlanDose = AugPlanDose(2:end, 2:end);  
    AugFilmDose = AugFilmDose(2:end, 2:end);  

    % Convert the ProfilePoint to physical coordinates in TPS space
    if (ProfilePoint(1) ~= 0) && (ProfilePoint(2) ~= 0)
        Point_X_TPS_mm = film_x(round(ProfilePoint(2)));
        Point_Y_TPS_mm = film_y(round(ProfilePoint(1)));

    else
        Point_X_TPS_mm = ProfilePoint(1) * pixelspacing(1);  
        Point_Y_TPS_mm = ProfilePoint(2) * pixelspacing(2);
    end
    
    % Find the closest index in both TPS and Film based on the physical mm position
    [~, t_x_idx] = min(abs(tps_x - Point_X_TPS_mm));  
    [~, t_y_idx] = min(abs(tps_y - Point_Y_TPS_mm)); 
    [~, f_x_idx] = min(abs(film_x - Point_X_TPS_mm));
    [~, f_y_idx] = min(abs(film_y - Point_Y_TPS_mm)); 

    % Extract profiles based on calculated indices
    tps_x_profile = AugPlanDose(t_x_idx, :); 
    tps_y_profile = AugPlanDose(:, t_y_idx); 

    film_x_profile = AugFilmDose(f_x_idx, : ); 
    film_y_profile = AugFilmDose(:,  f_y_idx);

    % Output profiles combining coordinates and extracted profiles
    tpsX = [tps_x, tps_y_profile];  
    tpsY = [tps_y', tps_x_profile'];  
    
    filmX = [film_x, film_y_profile];   
    filmY = [film_y', film_x_profile'];   

end

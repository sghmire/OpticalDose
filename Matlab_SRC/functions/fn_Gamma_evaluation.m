function [gamma_result, pass_rate] = fn_Gamma_evaluation(reference, evaluated, type, DTA, DD, threshold)
    % reference: TPS_augmented
    % evaluated: Film_augmented
    % DTA: Distance To Agreement criterion (in mm)
    % DD: Dose Difference criterion (in percent)
    % threshold: minimum dose to consider (percent of max dose)

    % Extract spatial information and dose matrices
    ref_x = reference(1, 2:end);
    ref_y = reference(2:end, 1);
    ref_dose = reference(2:end, 2:end);
    
    eval_x = evaluated(1, 2:end);
    eval_y = evaluated(2:end, 1);
    eval_dose = evaluated(2:end, 2:end);

    if strcmp(type, "Relative")  
        % Normalize doses to maximum reference dose
        max_ref_dose = max(ref_dose(:));
        ref_dose = ref_dose / max_ref_dose * 100;
        eval_dose = eval_dose / max_ref_dose * 100;
    end
        
    % Initialize gamma matrix
    gamma_result = zeros(size(eval_dose));
    
    
    % Determine the search window size based on DTA
    x_step = mean(diff(ref_x));
    y_step = mean(diff(ref_y));
    x_window = ceil(DTA / x_step);
    y_window = ceil(DTA / y_step);
    
    h = waitbar(0, "Computing gamma");

    % Loop through each point in the evaluated dose
    for i = 1:length(eval_y)
        waitbar((i/length(eval_y)), h, "Calculating!");
        for j = 1:length(eval_x)
            if eval_dose(i,j) < threshold
                continue;  % Skip low dose points
            end
            
            % Find the closest point in reference
            [~, ref_i] = min(abs(ref_y - eval_y(i)));
            [~, ref_j] = min(abs(ref_x - eval_x(j)));
            
            % Define search ranges
            i_range = max(1, ref_i-y_window):min(length(ref_y), ref_i+y_window);
            j_range = max(1, ref_j-x_window):min(length(ref_x), ref_j+x_window);
            
            % Extract subset of reference dose for comparison
            ref_subset = ref_dose(i_range, j_range);
            
            % Calculate dose difference and distance for subset
            dose_diff = abs(ref_subset - eval_dose(i,j));
            [X, Y] = meshgrid(ref_x(j_range) - eval_x(j), ref_y(i_range) - eval_y(i));
            distance = sqrt(X.^2 + Y.^2);
            
            % Calculate gamma
            gamma = sqrt((dose_diff/DD).^2 + (distance/DTA).^2);
            
            % Store the minimum gamma value
            gamma_result(i,j) = min(gamma(:));
        end
    end
    
    % Calculate pass rate
    pass_rate = nnz(gamma_result(:) <= 1) / numel(gamma_result) * 100;
    close(h);
end
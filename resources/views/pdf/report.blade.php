<!DOCTYPE html>
<html>
<head>
    <title>{{ $project->project_name }} Report</title>
    <style>
        body {
            font-family: 'Helvetica', sans-serif;
            font-size: 12px;
            color: #333;
            margin: 10px;
        }

        h1, h2, h3 {
            margin: 0 0 8px 0;
            color: #1f2937;
        }

        h1 { font-size: 20px; }
        h2 { font-size: 16px; }
        h3 { font-size: 14px; }

        .section {
            margin-bottom: 15px;
            page-break-inside: avoid;
        }

        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 10px;
            page-break-inside: avoid;
        }

        th, td {
            border: 1px solid #d1d5db;
            padding: 6px 8px;
            text-align: left;
        }

        th {
            background-color: #f3f4f6;
        }

        /* --- Badge Styling (For Status) --- */
        .badge {
            display: inline-block;
            padding: 2px 6px;
            border-radius: 4px;
            color: #fff;
            font-size: 10px;
            font-weight: bold;
        }

        .badge-success { background-color: #16a34a; }
        .badge-warning { background-color: #f59e0b; }
        .badge-danger  { background-color: #dc2626; }
        .badge-primary { background-color: #3b82f6; }
        .badge-secondary { background-color: #6b7280; }

        /* --- Overall Progress Bar (Kept for overall completion) --- */
        .overall-progress-bar {
            background-color: #e5e7eb;
            border-radius: 6px;
            overflow: hidden;
            height: 12px;
            margin-top: 3px;
        }

        .overall-progress-fill {
            height: 100%;
            background-color: #3b82f6;
            text-align: right;
            padding-right: 3px;
            color: #fff;
            font-size: 10px;
            line-height: 12px;
        }
        
        /* --- Chart Styling (Shared for Phases and SDGs) --- */
        .chart-cell {
            padding: 2px 8px;
            vertical-align: middle;
        }
        
        .progress-bar-container { /* Renamed for general use */
            height: 14px;
            width: 100%;
            background-color: #e5e7eb;
            border-radius: 2px;
            overflow: hidden;
            position: relative;
        }
        
        .progress-bar-fill { /* Renamed for general use */
            height: 100%;
            border-radius: 2px;
            transition: width 0.5s ease-in-out;
            position: relative;
        }

        .mark-column {
            width: 150px;
            text-align: center;
            font-weight: bold;
        }

        /* --- Pie Chart Container Styling --- */
        .pie-chart-container {
            width: 100%;
            text-align: center;
            margin-top: 15px;
            margin-bottom: 15px;
        }

        .pie-chart-container img {
            max-width: 250px;
            height: auto;
            border: 1px solid #eee;
            box-shadow: 2px 2px 5px rgba(0,0,0,0.1);
        }

        /* Force everything to fit on one page */
        @page {
            size: A4;
            margin: 10mm;
        }

    </style>
</head>
<body>
    <div class="section">
        <h1>Project Report</h1>
        <div class="logo">
    <img src="{{ public_path('img/logo.jpg') }}" alt="Logo" style="width: 200px; height: auto;">

    </div>

        <h2>{{ $project->project_name }}</h2>
        <p><strong>Location:</strong> {{ $project->project_location }} | 
            <strong>Reg Date:</strong> {{ $project->reg_date }} | 
            <strong>PIC:</strong> {{ $project->pic_contact }}</p>
        <p><strong>Target:</strong>
            @php
                $targetColor = match($project->target) {
                    'PLATINUM' => 'badge-success',
                    'GOLD' => 'badge-warning',
                    'SILVER' => 'badge-danger',
                    'CERTIFIED' => 'badge-primary',
                    default => 'badge-secondary',
                };
            @endphp
            <span class="badge {{ $targetColor }}">{{ $project->target ?? 'N/A' }}</span>
        </p>
    </div>

    @if($process)
        <div class="section">
            <h3>Total Score Breakdown: {{ $completion }}%</h3> 
            <div class="overall-progress-bar">
                <div class="overall-progress-fill" style="width: {{ $completion }}%;">{{ $completion }}%</div>
            </div>

            {{-- PHP Data for Phase Scores --}}
            @php
                // Defined Max Scores and a consistent color for phases (e.g., Primary Blue)
                $phase_details = [
                    'initiation' => ['max' => 17, 'color' => '#3b82f6', 'name' => 'Initiation'], 
                    'planning' => ['max' => 31, 'color' => '#3b82f6', 'name' => 'Planning'],
                    'execution' => ['max' => 60, 'color' => '#3b82f6', 'name' => 'Execution'],
                    'monitoring' => ['max' => 12, 'color' => '#3b82f6', 'name' => 'Monitoring'],
                    'closing' => ['max' => 14, 'color' => '#3b82f6', 'name' => 'Closing'],
                ];
                $phase_keys = array_keys($phase_details);
            @endphp

            <table>
                <thead>
                    <tr>
                        <th>Step</th>
                        <th class="mark-column">Score / Max</th>
                        <th>Achievement Progress</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach ($phase_keys as $phase_key)
                        @php
                            $max_score = $phase_details[$phase_key]['max'];
                            $color_hex = $phase_details[$phase_key]['color'];
                            $name = $phase_details[$phase_key]['name'];
                            
                            $score = $process->$phase_key ?? 0;

                            // Calculation
                            $width_percent = ($max_score > 0) ? round(($score / $max_score) * 100) : 0;
                            $width_style = "width: {$width_percent}%; background-color: {$color_hex};"; 
                        @endphp
                        <tr>
                            <td>{{ $name }}</td>
                            <td class="mark-column">{{ $score }}/{{ $max_score }} ({{ $width_percent }}%)</td>
                            <td class="chart-cell">
                                <div class="progress-bar-container">
                                    <div class="progress-bar-fill" style="{{ $width_style }}"></div>
                                </div>
                            </td>
                        </tr>
                    @endforeach
                    <tr>
                        <td><strong>Status</strong></td>
                        <td class="mark-column" colspan="2">
                            @php
                                $statusColor = match($process->status) {
                                    'PLATINUM' => 'badge-success',
                                    'GOLD' => 'badge-warning',
                                    'SILVER' => 'badge-primary',
                                    'CERTIFIED' => 'badge-success',
                                    'FAIL' => 'badge-danger',
                                    default => 'badge-secondary',
                                };
                            @endphp
                            <span class="badge {{ $statusColor }}">{{ $process->status }}</span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    @endif

    @if($sdgs)
        <div class="section">
            <h3>SDG Score</h3>
            <table>
                <thead>
                    <tr>
                        <th>SDG</th>
                        <th class="mark-column">Score / Max</th>
                        <th>Achievement Progress</th>
                    </tr>
                </thead>
                <tbody>
                    @php
                        // Defined Max Scores and Colors as per user request
                        $sdg_details = [
                            'sdg3' => ['max' => 10, 'color' => '#16a34a'], // Green
                            'sdg6' => ['max' => 4, 'color' => '#3b82f6'], // Blue
                            'sdg7' => ['max' => 9, 'color' => '#3b82f6'], // Blue
                            'sdg8' => ['max' => 7, 'color' => '#16a34a'], // Green
                            'sdg9' => ['max' => 32, 'color' => '#f59e0b'], // Yellow
                            'sdg11' => ['max' => 7, 'color' => '#f59e0b'], // Yellow
                            'sdg12' => ['max' => 30, 'color' => '#f59e0b'], // Yellow
                            'sdg13' => ['max' => 4, 'color' => '#dc2626'], // Red
                            'sdg15' => ['max' => 1, 'color' => '#3b82f6'], // Blue 
                        ];

                        $sdg_keys = array_keys($sdg_details);
                    @endphp

                    @foreach ($sdg_keys as $sdg_key)
                        @php
                            $max_score = $sdg_details[$sdg_key]['max'];
                            $color_hex = $sdg_details[$sdg_key]['color'];
                            
                            $score = $sdgs->$sdg_key ?? 0;

                            // Calculation
                            $width_percent = ($max_score > 0) ? round(($score / $max_score) * 100) : 0;
                            $width_style = "width: {$width_percent}%; background-color: {$color_hex};"; 
                        @endphp
                        <tr>
                            <td>{{ strtoupper(str_replace('sdg', 'SDG ', $sdg_key)) }}</td>
                            <td class="mark-column">{{ $score }}/{{ $max_score }} ({{ $width_percent }}%)</td>
                            <td class="chart-cell">
                                <div class="progress-bar-container">
                                    <div class="progress-bar-fill" style="{{ $width_style }}"></div>
                                </div>
                            </td>
                        </tr>
                    @endforeach
                    <tr>
                        <td><strong>Status</strong></td>
                        <td class="mark-column" colspan="2">
                            @php
                                $sdgStatusColor = match($sdgs->status) {
                                    'PLATINUM' => 'badge-success',
                                    'GOLD' => 'badge-warning',
                                    'SILVER' => 'badge-primary',
                                    'CERTIFIED' => 'badge-success',
                                    'FAIL' => 'badge-danger',
                                    default => 'badge-secondary',
                                };
                            @endphp
                            <span class="badge {{ $sdgStatusColor }}">{{ $sdgs->status }}</span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    @endif
</body>
</html>
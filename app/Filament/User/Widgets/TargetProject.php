<?php

namespace App\Filament\User\Widgets;

use Filament\Widgets\ChartWidget;
use App\Models\Process;
use Illuminate\Support\Facades\Auth;

class TargetProject extends ChartWidget
{
    protected ?string $heading = 'Target Project Recommendation';

    protected function getType(): string
    {
        return 'bar';
    }

    protected function getData(): array
    {
        $userId = Auth::id();

        $process = Process::whereHas('project', fn($q) => $q->where('user_id', $userId))
            ->latest()
            ->first();

        // Max marks per stage
        $maxMarks = [
            'Initiation' => 17,
            'Planning'   => 26,
            'Execution'  => 42,
            'Monitoring' => 12,
            'Closing'    => 14,
        ];

        // Current marks
        $currentMarks = $process ? [
            'Initiation' => $process->initiation,
            'Planning'   => $process->planning,
            'Execution'  => $process->execution,
            'Monitoring' => $process->monitoring,
            'Closing'    => $process->closing,
        ] : array_fill_keys(array_keys($maxMarks), 0);

        $totalMax = array_sum($maxMarks);
        $totalCurrent = array_sum($currentMarks);

        // Project target threshold
        $target = $process?->project?->target ?? 'N/A';

        $targetScore = match($target) {
            'PLATINUM' => 86,
            'GOLD'     => 76,
            'SILVER'   => 66,
            'CERTIFIED'=> 50,
            default    => 0,
        };

        // Compute required marks remaining
        $remainingScore = max(0, $targetScore - $totalCurrent);

        // Proportionally distribute remaining marks per stage
        $recommendedMarks = [];
        foreach ($maxMarks as $stage => $max) {
            $current = $currentMarks[$stage];
            $proportion = $max / $totalMax;
            $recommended = min($max, $current + round($remainingScore * $proportion));
            $recommendedMarks[$stage] = $recommended;
        }

        // Prepare labels and data
        $labels = [];
        $data = [];
        $colors = [];

        foreach ($maxMarks as $stage => $max) {
            $obtained = $currentMarks[$stage];
            $recommended = $recommendedMarks[$stage];
            $labels[] = "{$stage} ({$obtained}/{$max})";
            $data[] = $recommended;

            // Color: green if achieved target proportion, yellow if still needed
            $colors[] = $recommended >= $max ? '#22c55e' : '#facc15';
        }

        return [
            'labels' => $labels,
            'datasets' => [
                [
                    'label' => "Recommended Marks to Reach {$target}",
                    'data' => $data,
                    'backgroundColor' => $colors,
                ],
            ],
        ];
    }
}

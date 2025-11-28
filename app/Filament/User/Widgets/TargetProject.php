<?php

namespace App\Filament\User\Widgets;

use Filament\Widgets\ChartWidget;
use App\Models\Project;
use App\Models\Process;
use Illuminate\Support\Facades\Auth;

class TargetProject extends ChartWidget
{
    protected ?string $heading = 'Target Project Recommendation';

    public ?int $projectId = null;

    protected $listeners = ['projectSelected' => 'updateProject'];

    public function updateProject($projectId)
    {
        $this->projectId = $projectId;
        $this->dispatch('$refresh');
    }

    public function getHeading(): string
    {
        if ($this->projectId) {
            $p = Project::find($this->projectId);
            return $p?->project_name ?? 'Project Process Completion';
        }

        $project = Project::where('user_id', Auth::id())->latest()->first();
        return $project?->project_name ?? 'Project Process Completion';
    }

    protected function getType(): string
    {
        return 'bar';
    }

    protected function getData(): array
    {
        $userId = Auth::id();

        $process = Process::when(
            $this->projectId,
            fn($q) => $q->where('project_id', $this->projectId)
        )
        ->when(
            !$this->projectId,
            fn($q) => $q->whereHas('project', fn($p) => $p->where('user_id', $userId))->latest()
        )
        ->first();

        // Max marks per phase
        $maxMarks = [
            'Initiation' => 17,
            'Planning'   => 31,
            'Execution'  => 60,
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

        // Determine current status
        $processStatus = match (true) {
            $totalCurrent >= 86 => 'PLATINUM',
            $totalCurrent >= 76 => 'GOLD',
            $totalCurrent >= 66 => 'SILVER',
            $totalCurrent >= 50 => 'CERTIFIED',
            default => 'FAIL',
        };

        // Project target threshold
        $target = $process?->project?->target ?? 'N/A';

        $targetScore = match ($target) {
            'PLATINUM' => 86,
            'GOLD'     => 76,
            'SILVER'   => 66,
            'CERTIFIED'=> 50,
            default    => 0,
        };

        // Required marks to reach target
        $remainingScore = max(0, $targetScore - $totalCurrent);

        // ----------------------------
        // Minimum Recommended Marks Logic
        // ----------------------------
        $recommendedMarks = [];

        foreach ($maxMarks as $stage => $max) {
            $current = $currentMarks[$stage];

            // Weight based on stage max marks
            $proportion = $max / $totalMax;

            // Add proportional required marks
            $recommended = $current + round($remainingScore * $proportion);

            // Cap at maximum possible
            $recommendedMarks[$stage] = min($recommended, $max);
        }

        // Chart Data
        $labels = [];
        $data = [];
        $colors = [];

        foreach ($maxMarks as $stage => $max) {
            $obtained = $currentMarks[$stage];
            $recommended = $recommendedMarks[$stage];

            $labels[] = "{$stage} ({$obtained}/{$max})";
            $data[] = $recommended;

            // Colors: Green = complete, Yellow = needs improvement
            $colors[] = $recommended >= $max ? '#22c55e' : '#facc15';
        }

        return [
            'labels' => $labels,
            'datasets' => [
                [
                    'label' => "Minimum Marks Needed to Reach {$target} (Current Status: {$processStatus})",
                    'data' => $data,
                    'backgroundColor' => $colors,
                ],
            ],
        ];
    }
}

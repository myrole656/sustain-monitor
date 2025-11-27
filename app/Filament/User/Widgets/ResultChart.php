<?php

namespace App\Filament\User\Widgets;

use Filament\Widgets\ChartWidget;
use App\Models\Project;
use App\Models\Process;
use Illuminate\Support\Facades\Auth;

class ResultChart extends ChartWidget
{
    protected bool $hasForm = false;
    protected static bool $isFormHidden = true;

    public ?int $project_id = null;   // keep your original variable
    public ?int $projectId = null;    // ADD THIS (required)

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

    public function getType(): string
    {
        return 'doughnut';
    }

    public function getData(): array
    {
        $userId = Auth::id();

        $process = Process::query()
            ->when($this->projectId, fn($q) => $q->where('project_id', $this->projectId))
            ->when(!$this->projectId, fn($q) =>
                $q->whereHas('project', fn($p) => $p->where('user_id', $userId))
                    ->latest()
            )
            ->first();

        $maxMarks = [
            'Initiation' => 17,
            'Planning'   => 31,
            'Execution'  => 60,
            'Monitoring' => 12,
            'Closing'    => 14,
        ];

        $stepMarks = $process ? [
            'Initiation' => $process->initiation,
            'Planning'   => $process->planning,
            'Execution'  => $process->execution,
            'Monitoring' => $process->monitoring,
            'Closing'    => $process->closing,
        ] : array_fill_keys(array_keys($maxMarks), 0);

        $totalMax = array_sum($maxMarks);
        $obtained = array_sum($stepMarks);
        $overallPercentage = round(($obtained / $totalMax) * 100, 1);

        $labels = [];
        $percentages = [];

        foreach ($stepMarks as $step => $mark) {
            $labels[] = "{$step} ({$mark}/{$maxMarks[$step]})";
            $percentages[] = round(($mark / $totalMax) * 100, 1);
        }

        $remaining = max(0, 100 - array_sum($percentages));

        return [
            'labels' => array_merge($labels, ['Remaining']),
            'datasets' => [
                [
                    'label' => 'Project Completion (%)',
                    'data' => array_merge($percentages, [$remaining]),
                    'backgroundColor' => [
                        '#22c55e',
                        '#ef4444',
                        '#facc15',
                        '#3c2060',
                        '#0ea5e9',
                        '#e5e7eb',
                    ],
                    'hoverOffset' => 12,
                ],
            ],
            'overallPercentage' => $overallPercentage,
        ];
    }
}

<?php

namespace App\Filament\Resources\Projects\Pages;

use App\Filament\Resources\Projects\ProjectResource;
use Filament\Resources\Pages\EditRecord;
use App\Models\Process;
use App\Models\SDGStatus;

class EditProject extends EditRecord
{
    protected static string $resource = ProjectResource::class;

    protected array $processData = [];
    protected array $sdgTotals = [];

    protected function mutateFormDataBeforeSave(array $data): array
    {
        // Capture process data and remove from main payload
        $this->processData = $data['process'] ?? [];
        unset($data['process']);

        // Initialize SDG totals
        $this->sdgTotals = [
            'sdg3'  => 0,
            'sdg6'  => 0,
            'sdg7'  => 0,
            'sdg8'  => 0,
            'sdg9'  => 0,
            'sdg11' => 0,
            'sdg12' => 0,
            'sdg13' => 0,
            'sdg15' => 0,
        ];

        // SDG mapping (same as CreateProject)
        $sdgMap = [
            // Initiation
            'site_selection' => 'sdg3',
            'energy_preassessment' => 'sdg3',
            'water_strategy' => 'sdg3',
            'waste_plan' => 'sdg3',
            'sustainability_statement' => 'sdg3',
            'bim_simulation' => null,
            'environment_simulations' => null,
            'clash_free_bim' => null,

            // Planning
            'energy_modelling' => 'sdg3',
            'high_eff' => 'sdg3',
            'passive_design' => null,
            'rain_harvest' => null,
            'greywater' => null,
            'daylight' => null,
            'ventiation' => null,
            'low_carbon' => null,
            'const_waste' => null,
            'green_land' => null,
            'heat_strategy' => null,
            'thermal_model' => null,
            'water_eff' => null,
            'mats_lifecycle' => null,

            // Execution
            'basic_access' => 'sdg3',
            'smart_energy' => null,
            'ctrl_measure' => null,

            // Monitoring
            'sustain' => 'sdg3',

            // Closing
            'recyclable_percentage' => 'sdg3',
        ];

        // helper to add SDG points
        $addSdgPoints = function (string $item, int $points = 1) use (&$sdgMap) {
            $sdgColumn = $sdgMap[$item] ?? null;
            return $sdgColumn ? [$sdgColumn => $points] : [];
        };

        // Loop over each phase in processData
        foreach ($this->processData as $phase => $items) {
            if (!is_array($items)) continue;

            foreach ($items as $item => $value) {
                $points = is_numeric($value) ? (int)$value : 1;
                $sdgAdd = $addSdgPoints($item, $points);
                foreach ($sdgAdd as $col => $val) {
                    $this->sdgTotals[$col] = ($this->sdgTotals[$col] ?? 0) + $val;
                }
            }
        }

        return $data;
    }

    protected function afterSave(): void
    {
        $project = $this->record;

        if (!empty($this->processData)) {
            $score = (
                (int) ($this->processData['initiation'] ?? 0) +
                (int) ($this->processData['planning'] ?? 0) +
                (int) ($this->processData['monitoring'] ?? 0) +
                (int) ($this->processData['execution'] ?? 0) +
                (int) ($this->processData['closing'] ?? 0)
            );

            $status = match (true) {
                $score >= 86 => 'PLATINUM',
                $score >= 76 => 'GOLD',
                $score >= 66 => 'SILVER',
                $score >= 50 => 'CERTIFIED',
                default => 'FAIL',
            };

            Process::updateOrCreate(
                ['project_id' => $project->id],
                [
                    'initiation' => $this->processData['initiation'] ?? 0,
                    'planning'   => $this->processData['planning'] ?? 0,
                    'monitoring' => $this->processData['monitoring'] ?? 0,
                    'execution'  => $this->processData['execution'] ?? 0,
                    'closing'    => $this->processData['closing'] ?? 0,
                    'status'     => $status,
                ]
            );
        }

        // Update or create SDGStatus with recalculated totals
        $sdgPayload = array_merge([
            'project_id' => $project->id,
            'sdg3'  => 0,
            'sdg6'  => 0,
            'sdg7'  => 0,
            'sdg8'  => 0,
            'sdg9'  => 0,
            'sdg11' => 0,
            'sdg12' => 0,
            'sdg13' => 0,
            'sdg15' => 0,
            'status' => null,
        ], $this->sdgTotals);

        SDGStatus::updateOrCreate(
            ['project_id' => $project->id],
            $sdgPayload
        );
    }
}

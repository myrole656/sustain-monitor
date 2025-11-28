<?php

namespace App\Filament\Resources\Projects\Pages;

use Filament\Resources\Pages\CreateRecord;
use App\Filament\Resources\Projects\ProjectResource;
use App\Models\Process;
use App\Models\Project;
use App\Models\SDGStatus;
use Filament\Exceptions\FormsValidationException;

class CreateProject extends CreateRecord
{
    protected static string $resource = ProjectResource::class;
    protected static bool $canCreateAnother = false;

    protected array $processData = [];
    protected array $sdgTotals = [];

    protected function mutateFormDataBeforeCreate(array $data): array
    {
        // Prevent duplicate project name for this user
        if (Project::where('user_id', auth()->id())
            ->where('project_name', $data['project_name'])
            ->exists()
        ) {
            throw FormsValidationException::withMessages([
                'project_name' => 'You already have a project with this name. Please make a new one or edit existing project',
            ]);
        }

        // initialize
        $this->processData = [];
        $this->sdgTotals = [
            'sdg3'  => 0, 'sdg6'  => 0, 'sdg7'  => 0,
            'sdg8'  => 0, 'sdg9'  => 0, 'sdg11' => 0,
            'sdg12' => 0, 'sdg13' => 0, 'sdg15' => 0,
        ];

        $sdgMaxPoints = [
            'sdg3' => 10, 'sdg6' => 4, 'sdg7' => 9, 'sdg8' => 7,
            'sdg9' => 32, 'sdg11' => 7, 'sdg12' => 30, 'sdg13' => 4, 'sdg15' => 1,
        ];

        // SDG mapping (same as your original)
        $sdgMap = [
            'site_selection' => 'sdg11,sdg15',
            'energy_preassessment' => 'sdg13',
            'water_strategy' => 'sdg11',
            'waste_plan' => 'sdg11',
            'sustainability_statement' => 'sdg11',
            'bim_simulation' => 'sdg13',
            'environment_simulations' => 'sdg13',
            'clash_free_bim' => 'sdg11',

            'energy_modelling' => 'sdg7,sdg9',
            'high_eff' => 'sdg3,sdg7,sdg9',
            'passive_design' => 'sdg7,sdg9',
            'rain_harvest' => 'sdg6,sdg9',
            'greywater' => 'sdg6,sdg9',
            'daylight' => 'sdg3,sdg7,sdg9',
            'ventiation' => 'sdg3,sdg7,sdg9',
            'low_carbon' => 'sdg3,sdg7,sdg9',
            'const_waste' => 'sdg3,sdg9',
            'green_land' => 'sdg3,sdg6',
            'heat_strategy' => 'sdg3,sdg7,sdg9',
            'thermal_model' => 'sdg7,sdg9',
            'water_eff' => 'sdg6',
            'mats_lifecycle' => 'sdg3,sdg9',

            'smart_energy' => 'sdg8,sdg12',
            'ctrl_measure' => 'sdg9,sdg12',
            'li_design' => 'sdg9,sdg12',
            'se_ctrl' => 'sdg9,sdg12',
            'avoid' => 'sdg9,sdg12',
            'basic_access' => 'sdg8,sdg9',
            'waste_tracking' => 'sdg9,sdg12',
            'perecentage_recycled' => 'sdg12',
            'my_hijau' => 'sdg9,sdg12',
            'useof_recycle' => 'sdg8,sdg12',
            'recyce_rate' => 'sdg12',
            'reduce_waste' => 'sdg9,sdg12',
            'voc' => 'sdg8,sdg12',
            'led' => 'sdg9,sdg12',
            'motion_sensors' => 'sdg9,sdg12',
            'hvac_systems' => 'sdg9,sdg12',
            'renewable_energy' => 'sdg9,sdg12',
            'energy_management' => 'sdg9,sdg12',
            'energy_star' => 'sdg9',
            'bas_integrated' => 'sdg9',
            'energy_eff' => 'sdg9,sdg12',
            'tolerat_landscape' => 'sdg12',
            'bathroom_pantry' => 'sdg12',
            'water_consumpt' => 'sdg12',
            'water_monitor' => 'sdg9,sdg12',
            'water_leakage' => 'sdg9,sdg12',
            'ventilation_light' => 'sdg8,sdg12',
            'ieq_standard' => 'sdg8,sdg9,sdg12',

            'build_performance' => 'sdg11',
            'air_quaity' => 'sdg3',
            'thermal_comfort' => 'sdg3',
            'cleaning_mats' => 'sdg11',
            'sustain' => 'sdg7',

            'mats_salvage' => 'sdg12',
            'recyclable_percentage' => 'sdg12',
            'waste_segregation' => 'sdg12',
            'audit_docs' => 'sdg8',
            'recycle_analysis' => 'sdg12',
            'eol_impact' => 'sdg13',
        ];

        // Helper: calculate stage totals and per-sdg contributions
        $calculateStage = function(array $items, array $marks) use ($sdgMap) {
            $total = 0;
            $sdgPoints = [];
            foreach ($items as $item) {
                $points = $marks[$item] ?? 0;
                $total += $points;
                if (!empty($sdgMap[$item])) {
                    $sdgs = array_map('trim', explode(',', $sdgMap[$item]));
                    $splitPoints = $points / count($sdgs);
                    foreach ($sdgs as $sdg) {
                        $sdgPoints[$sdg] = ($sdgPoints[$sdg] ?? 0) + $splitPoints;
                    }
                }
            }
            return [$total, $sdgPoints];
        };

        // Marks arrays (same as original)
        $marksInitiation = [
            'site_selection'=>2,'energy_preassessment'=>2,'water_strategy'=>2,
            'waste_plan'=>2,'sustainability_statement'=>1,'bim_simulation'=>3,
            'environment_simulations'=>3,'clash_free_bim'=>2
        ];
        $marksPlanning = [
            'energy_modelling'=>3,'passive_design'=>2,'high_eff'=>2,
            'rain_harvest'=>2,'greywater'=>2,'daylight'=>2,'ventiation'=>2,
            'low_carbon'=>2,'const_waste'=>2,'green_land'=>2,'heat_strategy'=>2,
            'thermal_model'=>3,'water_eff'=>2,'mats_lifecycle'=>3
        ];
        $marksExecution = [
            'smart_energy'=>2,'ctrl_measure'=>2,'li_design'=>2,'se_ctrl'=>2,'avoid'=>1,
            'basic_access'=>1,'waste_tracking'=>3,'perecentage_recycled'=>2,'my_hijau'=>2,
            'useof_recycle'=>2,'recyce_rate'=>3,'reduce_waste'=>2,'voc'=>2,'led'=>2,
            'motion_sensors'=>2,'hvac_systems'=>2,'renewable_energy'=>3,'energy_management'=>3,
            'energy_star'=>3,'bas_integrated'=>3,'energy_eff'=>3,'tolerat_landscape'=>1,
            'bathroom_pantry'=>2,'water_eff'=>2,'water_consumpt'=>1,'water_monitor'=>2,
            'water_leakage'=>2,'ventilation_light'=>1,'ieq_standard'=>2
        ];
        $marksMonitoring = [
            'smart_energy'=>2,'build_performance'=>2,'air_quaity'=>2,'thermal_comfort'=>2,
            'cleaning_mats'=>2,'sustain'=>2
        ];
        $marksClosing = [
            'mats_salvage'=>2,'recyclable_percentage'=>3,'waste_segregation'=>2,
            'audit_docs'=>2,'recycle_analysis'=>3,'eol_impact'=>2
        ];

        // --------------------------
        // Initiation (from form field 'initiation_items')
        // --------------------------
        [$this->processData['initiation'], $sdgStage] =
            $calculateStage($data['initiation_items'] ?? [], $marksInitiation);
        foreach ($sdgStage as $k => $v) $this->sdgTotals[$k] += $v;

        // --------------------------
        // Planning (form uses 'planning_items')
        // --------------------------
        $planningItems = $data['planning_items'] ?? [];
        [$this->processData['planning'], $sdgStage] =
            $calculateStage($planningItems, $marksPlanning);
        foreach ($sdgStage as $k => $v) $this->sdgTotals[$k] += $v;

        // --------------------------
        // Execution (merge all process.execution_* arrays)
        // Form sends execution under process.execution_env, process.execution_waste, etc.
        // --------------------------
        $executionItems = array_merge(
            $data['process']['execution_env'] ?? [],
            $data['process']['execution_waste'] ?? [],
            $data['process']['execution_energy'] ?? [],
            $data['process']['execution_water'] ?? [],
            $data['process']['execution_ieq'] ?? [],
        );
        [$this->processData['execution'], $sdgStage] =
            $calculateStage($executionItems, $marksExecution);
        foreach ($sdgStage as $k => $v) $this->sdgTotals[$k] += $v;

        // --------------------------
        // Monitoring (process.monitoring)
        // --------------------------
        $monitoringItems = $data['process']['monitoring'] ?? [];
        [$this->processData['monitoring'], $sdgStage] =
            $calculateStage($monitoringItems, $marksMonitoring);
        foreach ($sdgStage as $k => $v) $this->sdgTotals[$k] += $v;

        // --------------------------
        // Closing (process.closing)
        // --------------------------
        $closingItems = $data['process']['closing'] ?? [];
        [$this->processData['closing'], $sdgStage] =
            $calculateStage($closingItems, $marksClosing);
        foreach ($sdgStage as $k => $v) $this->sdgTotals[$k] += $v;

        // cap SDG totals by max points and round
        foreach ($this->sdgTotals as $sdg => $points) {
            $this->sdgTotals[$sdg] = min(round($points), $sdgMaxPoints[$sdg]);
        }

        // remove large arrays from payload that are not needed in projects table
        unset(
            $data['process'],
            $data['initiation_items'],
            $data['planning_items']
        );

        return $data;
    }

    protected function afterCreate(): void
    {
        $projectId = $this->record->id;

        // --- Process status ---
        $score = 0;
        foreach (['initiation','planning','execution','monitoring','closing'] as $phase) {
            $score += (int) ($this->processData[$phase] ?? 0);
        }

        $processStatus = match (true) {
            $score >= 86 => 'PLATINUM',
            $score >= 76 => 'GOLD',
            $score >= 66 => 'SILVER',
            $score >= 50 => 'CERTIFIED',
            default => 'FAIL',
        };

        Process::create([
            'project_id' => $projectId,
            'initiation' => (int) ($this->processData['initiation'] ?? 0),
            'planning'   => (int) ($this->processData['planning'] ?? 0),
            'execution'  => (int) ($this->processData['execution'] ?? 0),
            'monitoring' => (int) ($this->processData['monitoring'] ?? 0),
            'closing'    => (int) ($this->processData['closing'] ?? 0),
            'status'     => $processStatus,
        ]);

        // --- SDG status ---
        $sdgScore = array_sum(array_map(fn($v) => (int)$v, $this->sdgTotals));
        $sdgStatus = match (true) {
            $sdgScore >= 89 => 'A',
            $sdgScore >= 69 => 'B',
            $sdgScore >= 49 => 'C',
            $sdgScore >= 40 => 'D',
            default => 'FAIL',
        };

        SDGStatus::create(array_merge(
            ['project_id' => $projectId, 'status' => $sdgStatus],
            array_map(fn($v) => (int)$v, $this->sdgTotals)
        ));
    }
}

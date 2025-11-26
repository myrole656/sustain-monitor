<?php

namespace App\Filament\Resources\Projects\Pages;

use App\Filament\Resources\Projects\ProjectResource;
use Filament\Resources\Pages\EditRecord;
use App\Models\Process;

class EditProject extends EditRecord
{
    protected static string $resource = ProjectResource::class;

    protected array $processData = [];

    protected function mutateFormDataBeforeSave(array $data): array
    {
        // Capture process data and remove from main payload
        $this->processData = $data['process'] ?? [];
        unset($data['process']);

        return $data;
    }

    protected function afterSave(): void
    {
        $project = $this->record;

        // If no process data provided, do nothing
        if (empty($this->processData)) {
            return;
        }

        $score = (
            (int) ($this->processData['initiation'] ?? 0) +
            (int) ($this->processData['planning'] ?? 0) +
            (int) ($this->processData['monitoring'] ?? 0) +
            (int) ($this->processData['execution'] ?? 0) +
            (int) ($this->processData['closing'] ?? 0)
        );

        $status = match ($score) {
            5 => 'A',
            4 => 'B',
            3 => 'C',
            2 => 'D',
            default => 'E',
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
}
